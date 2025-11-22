using LECOMS.Data.DTOs.Order;
using LECOMS.Data.DTOs.Voucher;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    /// <summary>
    /// ================================
    /// PAYMENT SERVICE – PAYOS – FINAL
    /// ================================
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _uow;
        private readonly IShopWalletService _shopWallet;
        private readonly IVoucherService _voucherService;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IUnitOfWork uow,
            IShopWalletService shopWallet,
            IVoucherService voucherService,
            IConfiguration config,
            ILogger<PaymentService> logger)
        {
            _uow = uow;
            _shopWallet = shopWallet;
            _voucherService = voucherService;
            _config = config;
            _logger = logger;
        }

        // =========================================================
        // 1. RETRY PAYMENT LINK
        // =========================================================
        public async Task<string> CreatePaymentLinkAsync(string orderId)
        {
            var tx = await _uow.Transactions.GetByOrderIdAsync(orderId)
                ?? throw new InvalidOperationException("Transaction not found.");

            if (tx.Status == TransactionStatus.Completed)
                throw new InvalidOperationException("Order already paid.");

            // Load all orders
            var list = new List<Order>();
            foreach (var id in tx.OrderId.Split(',', StringSplitOptions.TrimEntries))
            {
                var o = await _uow.Orders.GetAsync(
                    x => x.Id == id,
                    includeProperties: "Details.Product,Details.Product.Images,Details.Product.Category,Shop,User");

                if (o != null)
                    list.Add(o);
            }

            if (!list.Any())
                throw new InvalidOperationException("No valid orders.");

            // Reset fields
            tx.PayOSOrderCode = null;
            tx.PayOSPaymentUrl = null;
            tx.PayOSTransactionId = null;
            tx.Status = TransactionStatus.Pending;

            await _uow.Transactions.UpdateAsync(tx);
            await _uow.CompleteAsync();

            return await CreatePayOSPaymentAsync(tx, list);
        }

        // =========================================================
        // 2. CREATE PAYMENT FOR MULTIPLE ORDERS
        // =========================================================
        public async Task<string> CreatePaymentLinkForMultipleOrdersAsync(string transactionId, List<Order> orders)
        {
            var tx = await _uow.Transactions.GetAsync(t => t.Id == transactionId)
                ?? throw new InvalidOperationException("Transaction not found.");

            return await CreatePayOSPaymentAsync(tx, orders);
        }

        // =========================================================
        // 3. HANDLE WEBHOOK
        // =========================================================
        public async Task<bool> HandlePayOSWebhookAsync(string body)
        {
            var webhook = JsonSerializer.Deserialize<PayOSWebhookData>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (webhook?.Data == null)
                return false;

            var orderCode = webhook.Data.OrderCode;

            var tx = await _uow.Transactions.GetAsync(t => t.PayOSOrderCode == orderCode);
            if (tx == null)
                return false;

            if (tx.Status == TransactionStatus.Completed)
                return true;

            // Load all orders
            var orders = new List<Order>();
            foreach (var oid in tx.OrderId.Split(',', StringSplitOptions.TrimEntries))
            {
                var o = await _uow.Orders.GetAsync(
                    x => x.Id == oid,
                    includeProperties: "Shop,User");

                if (o != null)
                    orders.Add(o);
            }

            if (!orders.Any())
                return false;

            if (webhook.Code == "00")
                await HandlePaymentSuccessAsync(tx, orders, webhook);
            else
                await HandlePaymentFailedAsync(tx, orders);

            await _uow.CompleteAsync();
            return true;
        }

        // =========================================================
        // 4. SIGNATURE VERIFY
        // =========================================================
        public async Task<bool> VerifyPayOSSignatureAsync(string data, string signature)
        {
            var key = _config["PayOS:ChecksumKey"];
            var computed = ComputeHmacSha256(data, key);
            return computed.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }

        // =========================================================
        // 5. GET STATUS
        // =========================================================
        public async Task<Transaction?> GetTransactionStatusAsync(string orderId)
        {
            return await _uow.Transactions.GetByOrderIdAsync(orderId);
        }

        // =========================================================
        // 6. CANCEL PAYMENT
        // =========================================================
        public async Task<bool> CancelPaymentAsync(string orderId)
        {
            var tx = await _uow.Transactions.GetByOrderIdAsync(orderId);
            if (tx == null || tx.Status != TransactionStatus.Pending)
                return false;

            tx.Status = TransactionStatus.Cancelled;
            await _uow.Transactions.UpdateAsync(tx);
            await _uow.CompleteAsync();

            return true;
        }

        // =========================================================
        // 7. MAIN PAYOS PAYMENT CREATOR
        // =========================================================
        private async Task<string> CreatePayOSPaymentAsync(Transaction tx, List<Order> orders)
        {
            string clientId = _config["PayOS:ClientId"]!;
            string apiKey = _config["PayOS:ApiKey"]!;
            string checksumKey = _config["PayOS:ChecksumKey"]!;

            long orderCode = GenerateUniqueOrderCode();
            tx.PayOSOrderCode = orderCode;
            await _uow.Transactions.UpdateAsync(tx);
            await _uow.CompleteAsync();

            // Build item list
            var items = new List<object>();
            decimal shipping = orders.Sum(o => o.ShippingFee);
            decimal discount = orders.Sum(o => o.Discount);

            foreach (var o in orders)
            {
                foreach (var d in o.Details)
                {
                    items.Add(new
                    {
                        name = d.Product?.Name ?? "Product",
                        quantity = d.Quantity,
                        price = (int)d.UnitPrice
                    });
                }
            }

            if (shipping > 0)
                items.Add(new { name = "Shipping", quantity = 1, price = (int)shipping });

            if (discount > 0)
                items.Add(new { name = "Discount", quantity = 1, price = -(int)discount });

            // Build payload
            var payload = new
            {
                orderCode = orderCode,
                amount = (int)tx.TotalAmount,
                description = $"Order {orders.Count}",
                returnUrl = _config["PayOS:ReturnUrl"],
                cancelUrl = _config["PayOS:CancelUrl"],
                items
            };

            string signData =
                $"amount={payload.amount}&cancelUrl={payload.cancelUrl}&description={payload.description}&orderCode={payload.orderCode}&returnUrl={payload.returnUrl}";

            string signature = ComputeHmacSha256(signData, checksumKey);

            var reqBody = JsonSerializer.Serialize(new
            {
                payload.orderCode,
                payload.amount,
                payload.description,
                payload.returnUrl,
                payload.cancelUrl,
                signature,
                items
            });

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-client-id", clientId);
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);

            var res = await client.PostAsync(
                "https://api-merchant.payos.vn/v2/payment-requests",
                new StringContent(reqBody, Encoding.UTF8, "application/json"));

            var resBody = await res.Content.ReadAsStringAsync();

            var parsed = JsonSerializer.Deserialize<PayOSCreateResponse>(
                resBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed == null || parsed.Code != "00")
                throw new Exception($"PayOS error {parsed?.Desc}");

            tx.PayOSPaymentUrl = parsed.Data!.CheckoutUrl;
            await _uow.Transactions.UpdateAsync(tx);
            await _uow.CompleteAsync();

            return parsed.Data.CheckoutUrl;
        }

        // =========================================================
        // 8. PAYMENT SUCCESS
        // =========================================================
        private async Task HandlePaymentSuccessAsync(
            Transaction tx,
            List<Order> orders,
            PayOSWebhookData webhook)
        {
            tx.Status = TransactionStatus.Completed;
            tx.PayOSTransactionId = webhook.Data?.Reference ?? webhook.Data?.PaymentLinkId;
            tx.CompletedAt = DateTime.UtcNow;

            await _uow.Transactions.UpdateAsync(tx);

            var cfg = await _uow.PlatformConfigs.GetConfigAsync();

            foreach (var o in orders)
            {
                o.PaymentStatus = PaymentStatus.Paid;
                o.Status = OrderStatus.Processing;

                await _uow.Orders.UpdateAsync(o);

                decimal fee = o.Total * cfg.DefaultCommissionRate / 100;
                decimal shopReceive = o.Total - fee;

                await _shopWallet.AddPendingBalanceAsync(
                    o.ShopId,
                    shopReceive,
                    o.Id,
                    $"Order {o.OrderCode} revenue");
            }

            // Voucher
            if (!string.IsNullOrWhiteSpace(tx.VoucherCode))
            {
                await _voucherService.MarkVoucherUsedAsync(
                    orders.First().UserId,
                    tx.VoucherCode,
                    orders,
                    tx.PayOSTransactionId!);
            }
        }

        // =========================================================
        // 9. PAYMENT FAILED
        // =========================================================
        private async Task HandlePaymentFailedAsync(Transaction tx, List<Order> orders)
        {
            tx.Status = TransactionStatus.Failed;
            await _uow.Transactions.UpdateAsync(tx);

            foreach (var o in orders)
            {
                o.PaymentStatus = PaymentStatus.Failed;
                o.Status = OrderStatus.Cancelled;
                await _uow.Orders.UpdateAsync(o);
            }
        }

        // =========================================================
        // 10. CREATE PAYMENT RESULT FOR EXISTING ORDERS
        // =========================================================
        public async Task<CheckoutResultDTO> CreatePaymentResultForExistingOrdersAsync(string orderId)
        {
            var tx = await _uow.Transactions.GetByOrderIdAsync(orderId)
                ?? throw new InvalidOperationException("Transaction not found.");

            var ids = tx.OrderId.Split(',', StringSplitOptions.TrimEntries);
            var orders = new List<Order>();

            foreach (var id in ids)
            {
                var o = await _uow.Orders.GetAsync(
                    x => x.Id == id,
                    includeProperties: "Details.Product,Details.Product.Images,Shop,User,Details.Product.Category");

                if (o != null)
                    orders.Add(o);
            }

            var url = await CreatePaymentLinkForMultipleOrdersAsync(tx.Id, orders);

            // Map DTO
            var orderDtos = orders.Select(o => new OrderDTO
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                UserId = o.UserId,
                ShopId = o.ShopId,
                ShopName = o.Shop?.Name,
                CustomerName = o.User?.FullName,
                ShipToName = o.ShipToName,
                ShipToPhone = o.ShipToPhone,
                ShipToAddress = o.ShipToAddress,
                Subtotal = o.Subtotal,
                ShippingFee = o.ShippingFee,
                Discount = o.Discount,
                Total = o.Total,
                Status = o.Status.ToString(),
                PaymentStatus = o.PaymentStatus.ToString(),
                Details = o.Details.Select(d => new OrderDetailDTO
                {
                    Id = d.Id,
                    ProductId = d.ProductId,
                    ProductName = d.Product?.Name,
                    UnitPrice = d.UnitPrice,
                    Quantity = d.Quantity,
                    ProductCategory = d.Product?.Category?.Name,
                    ProductImage = d.Product?.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                }).ToList()
            }).ToList();

            return new CheckoutResultDTO
            {
                Orders = orderDtos,
                PaymentMethod = "PAYOS",
                PaymentUrl = url,
                TotalAmount = orders.Sum(o => o.Total),
                ShippingFee = orders.Sum(o => o.ShippingFee),
                DiscountApplied = orders.Sum(o => o.Discount),
                WalletAmountUsed = 0,
                VoucherCodeUsed = tx.VoucherCode,
                PayOSAmountRequired = orders.Sum(o => o.Total)
            };
        }

        // =========================================================
        // UTILITIES
        // =========================================================
        private long GenerateUniqueOrderCode()
        {
            long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int rnd = new Random().Next(100, 999);
            return (ts % 10000000) * 1000 + rnd;
        }

        private string ComputeHmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            return BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)))
                .Replace("-", "")
                .ToLower();
        }
    }

    // =========================================================
    // FULL PAYOS DTO
    // =========================================================

    public class PayOSCreateResponse
    {
        public string Code { get; set; } = "";
        public string Desc { get; set; } = "";
        public PayOSData? Data { get; set; }
        public string? Signature { get; set; }
    }

    public class PayOSData
    {
        public string? Bin { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public int Amount { get; set; }
        public string? Description { get; set; }
        public long OrderCode { get; set; }
        public string? Currency { get; set; }
        public string? PaymentLinkId { get; set; }
        public string? Status { get; set; }
        public string CheckoutUrl { get; set; } = "";
        public string? QrCode { get; set; }
    }

    public class PayOSWebhookData
    {
        public string Code { get; set; } = "";
        public string Desc { get; set; } = "";
        public PayOSWebhookDetail? Data { get; set; }
        public string? Signature { get; set; }
    }

    public class PayOSWebhookDetail
    {
        public long OrderCode { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? AccountNumber { get; set; }
        public string? Reference { get; set; }
        public string? TransactionDateTime { get; set; }
        public string? Currency { get; set; }
        public string? PaymentLinkId { get; set; }
        public string? Code { get; set; }
        public string? Desc { get; set; }
        public string? CounterAccountBankId { get; set; }
        public string? CounterAccountBankName { get; set; }
        public string? CounterAccountName { get; set; }
        public string? CounterAccountNumber { get; set; }
        public string? VirtualAccountName { get; set; }
        public string? VirtualAccountNumber { get; set; }
    }
}
