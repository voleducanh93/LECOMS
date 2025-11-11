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
    /// Service implementation cho Payment với PayOS - SÀN THU HỘ
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IShopWalletService _shopWalletService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly HttpClient _httpClient;

        public PaymentService(
            IUnitOfWork unitOfWork,
            IShopWalletService shopWalletService,
            IConfiguration configuration,
            ILogger<PaymentService> logger,
            HttpClient httpClient)
        {
            _unitOfWork = unitOfWork;
            _shopWalletService = shopWalletService;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Tạo payment link cho 1 order đơn lẻ
        /// Use case: Retry payment, admin manual generation
        /// </summary>
        public async Task<string> CreatePaymentLinkAsync(string orderId)
        {
            _logger.LogInformation("=== START CreatePaymentLinkAsync for Order: {OrderId} ===", orderId);

            try
            {
                // 1. Lấy order với Details
                var order = await _unitOfWork.Orders.GetAsync(
                    o => o.Id == orderId,
                    includeProperties: "Shop,User,Details.Product");

                if (order == null)
                {
                    _logger.LogError("❌ Order not found: {OrderId}", orderId);
                    throw new InvalidOperationException($"Order {orderId} not found");
                }

                _logger.LogInformation("✅ Order found: {OrderCode}, Total: {Total}, Details: {Count}",
                    order.OrderCode, order.Total, order.Details?.Count ?? 0);

                if (order.PaymentStatus != PaymentStatus.Pending)
                {
                    _logger.LogError("❌ Order payment status is not Pending: {Status}", order.PaymentStatus);
                    throw new InvalidOperationException($"Order {orderId} payment status is {order.PaymentStatus}");
                }

                // 2. Lấy platform config
                var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

                if (config == null)
                {
                    _logger.LogError("❌ PlatformConfig not found");
                    throw new InvalidOperationException("Platform configuration not found");
                }

                // 3. Tính platform fee
                decimal platformFeeAmount = order.Total * config.DefaultCommissionRate / 100;
                decimal shopAmount = order.Total - platformFeeAmount;

                // 4. Tạo Transaction
                var transaction = new Transaction
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = orderId,
                    TotalAmount = order.Total,
                    PlatformFeePercent = config.DefaultCommissionRate,
                    PlatformFeeAmount = platformFeeAmount,
                    ShopAmount = shopAmount,
                    Status = TransactionStatus.Pending,
                    PaymentMethod = "PayOS",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("✅ Transaction created: {TransactionId}", transaction.Id);

                // 5. Call PayOS API
                string paymentUrl = await CreatePayOSPaymentAsync(transaction, new List<Order> { order });

                // 6. Update transaction
                transaction.PayOSPaymentUrl = paymentUrl;
                await _unitOfWork.Transactions.UpdateAsync(transaction);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("=== ✅ SUCCESS: Payment link created: {Url} ===", paymentUrl);

                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ❌ ERROR creating payment link for Order {OrderId} ===", orderId);
                throw;
            }
        }

        /// <summary>
        /// ⭐ Tạo payment link cho NHIỀU orders (sàn thu hộ)
        /// Use case: Normal checkout flow
        /// </summary>
        public async Task<string> CreatePaymentLinkForMultipleOrdersAsync(string transactionId, List<Order> orders)
        {
            _logger.LogInformation("=== START CreatePaymentLinkForMultipleOrders: Transaction={TxId}, Orders={Count} ===",
                transactionId, orders.Count);

            try
            {
                // 1. Lấy transaction
                var transaction = await _unitOfWork.Transactions.GetAsync(t => t.Id == transactionId);

                if (transaction == null)
                {
                    _logger.LogError("❌ Transaction not found: {TransactionId}", transactionId);
                    throw new InvalidOperationException($"Transaction {transactionId} not found");
                }

                _logger.LogInformation("✅ Transaction found: Amount={Amount:N0}, Fee={Fee:N0}, Orders={OrderIds}",
                    transaction.TotalAmount, transaction.PlatformFeeAmount, transaction.OrderId);

                // 2. Load all orders with details
                var orderIds = transaction.OrderId.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var allOrders = new List<Order>();

                foreach (var orderId in orderIds)
                {
                    var order = await _unitOfWork.Orders.GetAsync(
                        o => o.Id == orderId.Trim(),
                        includeProperties: "Details.Product,Shop");

                    if (order != null)
                    {
                        allOrders.Add(order);
                        _logger.LogInformation("  - Order {OrderCode}: {Total:N0} VND, Shop {ShopId}, Items: {Count}",
                            order.OrderCode, order.Total, order.ShopId, order.Details?.Count ?? 0);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Order not found: {OrderId}", orderId);
                    }
                }

                if (!allOrders.Any())
                {
                    throw new InvalidOperationException("No valid orders found for transaction");
                }

                // 3. Call PayOS API
                string paymentUrl = await CreatePayOSPaymentAsync(transaction, allOrders);

                // 4. Update transaction
                transaction.PayOSPaymentUrl = paymentUrl;
                await _unitOfWork.Transactions.UpdateAsync(transaction);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("=== ✅ SUCCESS: Payment link created for {Count} order(s): {Url} ===",
                    allOrders.Count, paymentUrl);

                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ❌ ERROR creating payment link for Transaction {TransactionId} ===", transactionId);
                throw;
            }
        }

        /// <summary>
        /// ⭐ Core method: Call PayOS API để tạo payment link
        /// Hỗ trợ cả single order và multiple orders
        /// </summary>
        private async Task<string> CreatePayOSPaymentAsync(Transaction transaction, List<Order> orders)
        {
            try
            {
                _logger.LogInformation("=== START CreatePayOSPaymentAsync ===");

                // Get PayOS config
                var clientId = _configuration["PayOS:ClientId"];
                var apiKey = _configuration["PayOS:ApiKey"];
                var checksumKey = _configuration["PayOS:ChecksumKey"];
                var returnUrl = _configuration["PayOS:ReturnUrl"];
                var cancelUrl = _configuration["PayOS:CancelUrl"];

                _logger.LogInformation("PayOS Config: ClientId={ClientId}..., ApiKey exists={HasKey}, ReturnUrl={ReturnUrl}",
                    clientId?.Substring(0, Math.Min(8, clientId.Length)),
                    !string.IsNullOrEmpty(apiKey),
                    returnUrl);

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("❌ PayOS credentials missing!");
                    throw new InvalidOperationException("PayOS credentials not configured");
                }

                // Generate unique order code
                long orderCode = GenerateOrderCode(transaction.Id);
                _logger.LogInformation("Generated PayOS OrderCode: {OrderCode}", orderCode);

                // Save orderCode to transaction
                transaction.PayOSOrderCode = orderCode;
                await _unitOfWork.Transactions.UpdateAsync(transaction);
                await _unitOfWork.CompleteAsync();

                // ⭐ Prepare items từ TẤT CẢ orders
                var items = new List<object>();
                int itemCount = 0;

                foreach (var order in orders)
                {
                    if (order.Details?.Any() == true)
                    {
                        foreach (var od in order.Details)
                        {
                            var itemName = od.Product?.Name ?? "Product";

                            // Limit name length (PayOS có giới hạn)
                            if (itemName.Length > 50)
                            {
                                itemName = itemName.Substring(0, 47) + "...";
                            }

                            items.Add(new
                            {
                                name = itemName,
                                quantity = od.Quantity,
                                price = (int)od.UnitPrice
                            });

                            itemCount++;
                        }
                    }
                }

                _logger.LogInformation("✅ Prepared {Count} items from {OrderCount} order(s)", itemCount, orders.Count);

                // Fallback nếu không có items
                if (!items.Any())
                {
                    _logger.LogWarning("⚠️ No order details found, using fallback item");
                    items.Add(new
                    {
                        name = "Cart Items",
                        quantity = 1,
                        price = (int)transaction.TotalAmount
                    });
                }

                // ✅ Create SHORT description (max 25 chars)
                var description = orders.Count == 1
                    ? $"DH {orders[0].OrderCode.Substring(3, Math.Min(14, orders[0].OrderCode.Length - 3))}"
                    : $"Cart {DateTime.UtcNow:yyMMddHHmm}";

                if (description.Length > 25)
                {
                    description = description.Substring(0, 25);
                }

                _logger.LogInformation("Description: '{Description}' ({Length} chars)", description, description.Length);

                // Create payment request
                var paymentRequest = new
                {
                    orderCode = orderCode,
                    amount = (int)transaction.TotalAmount,
                    description = description,
                    items = items,
                    returnUrl = returnUrl,
                    cancelUrl = cancelUrl
                };

                _logger.LogInformation("Payment Request: OrderCode={OrderCode}, Amount={Amount:N0}, Desc='{Desc}', Items={ItemCount}",
                    paymentRequest.orderCode, paymentRequest.amount, paymentRequest.description, items.Count);

                // Calculate signature
                var dataToSign = $"amount={paymentRequest.amount}&cancelUrl={cancelUrl}&description={paymentRequest.description}&orderCode={orderCode}&returnUrl={returnUrl}";

                _logger.LogDebug("Data to sign: {Data}", dataToSign);

                var signature = ComputeHmacSha256(dataToSign, checksumKey);

                _logger.LogDebug("Signature: {Signature}...", signature.Substring(0, Math.Min(20, signature.Length)));

                var requestWithSignature = new
                {
                    paymentRequest.orderCode,
                    paymentRequest.amount,
                    paymentRequest.description,
                    paymentRequest.items,
                    paymentRequest.returnUrl,
                    paymentRequest.cancelUrl,
                    signature = signature
                };

                // Call PayOS API
                var apiUrl = "https://api-merchant.payos.vn/v2/payment-requests";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

                var json = JsonSerializer.Serialize(requestWithSignature, new JsonSerializerOptions
                {
                    WriteIndented = false
                });

                _logger.LogInformation("Calling PayOS API: {Url}", apiUrl);
                _logger.LogDebug("Request JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("PayOS Response Status: {Status} ({StatusCode})",
                    response.StatusCode, (int)response.StatusCode);
                _logger.LogInformation("PayOS Response: {Response}", responseContent);

                // Parse response
                var responseObj = JsonSerializer.Deserialize<PayOSCreateResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Check response code (PayOS returns "00" for success)
                if (responseObj?.Code != "00")
                {
                    var errorMsg = responseObj?.Desc ?? "Unknown error";
                    _logger.LogError("❌ PayOS error: Code={Code}, Desc={Desc}", responseObj?.Code, errorMsg);
                    throw new Exception($"PayOS error (code {responseObj?.Code}): {errorMsg}");
                }

                if (responseObj.Data == null)
                {
                    _logger.LogError("❌ Invalid PayOS response: Data is null");
                    throw new Exception("Invalid PayOS response: Data is null");
                }

                // Update transaction với PayOS info
                transaction.PayOSTransactionId = responseObj.Data.PaymentLinkId;
                await _unitOfWork.Transactions.UpdateAsync(transaction);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("✅ PayOS Payment Link ID: {PaymentLinkId}", responseObj.Data.PaymentLinkId);
                _logger.LogInformation("✅ Checkout URL: {CheckoutUrl}", responseObj.Data.CheckoutUrl);

                return responseObj.Data.CheckoutUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ❌ ERROR in CreatePayOSPaymentAsync ===");
                _logger.LogError("Exception Type: {Type}", ex.GetType().Name);
                _logger.LogError("Exception Message: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Xử lý webhook callback từ PayOS
        /// </summary>
        public async Task<bool> HandlePayOSWebhookAsync(string webhookData)
        {
            _logger.LogInformation("=== RECEIVED PAYOS WEBHOOK ===");
            _logger.LogInformation("Webhook Raw Data: {Data}", webhookData);

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var webhook = JsonSerializer.Deserialize<PayOSWebhookData>(
                    webhookData,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (webhook?.Data == null)
                {
                    _logger.LogError("Invalid webhook data: Data is null");
                    return false;
                }

                _logger.LogInformation("Webhook Code: {Code}, OrderCode: {OrderCode}, Amount: {Amount}",
                    webhook.Code, webhook.Data.OrderCode, webhook.Data.Amount);

                // Find transaction by PayOSOrderCode
                var orderCode = webhook.Data.OrderCode;
                var transaction = await FindTransactionByOrderCode(orderCode);

                if (transaction == null)
                {
                    _logger.LogError("Transaction not found for PayOS OrderCode: {OrderCode}", orderCode);
                    return false;
                }

                _logger.LogInformation("Found Transaction: {TransactionId}, Orders: {OrderIds}",
                    transaction.Id, transaction.OrderId);

                // Check idempotency
                if (transaction.Status == TransactionStatus.Completed)
                {
                    _logger.LogInformation("Transaction already processed (idempotent): {TransactionId}", transaction.Id);
                    return true;
                }

                // Get all orders
                var orderIds = transaction.OrderId.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var orders = new List<Order>();

                foreach (var orderId in orderIds)
                {
                    var order = await _unitOfWork.Orders.GetAsync(
                        o => o.Id == orderId.Trim(),
                        includeProperties: "Shop,User");

                    if (order != null)
                    {
                        orders.Add(order);
                    }
                }

                if (!orders.Any())
                {
                    _logger.LogError("No orders found for transaction: {TransactionId}", transaction.Id);
                    return false;
                }

                // Process based on webhook code
                if (webhook.Code == "00") // Success
                {
                    await HandlePaymentSuccessAsync(transaction, orders, webhook);
                }
                else // Failed
                {
                    await HandlePaymentFailedAsync(transaction, orders, webhook);
                }

                await _unitOfWork.CompleteAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation("=== ✅ WEBHOOK PROCESSED for Transaction: {TransactionId} ===", transaction.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayOS webhook");
                await dbTransaction.RollbackAsync();
                return false;
            }
        }

        /// <summary>
        /// ⭐ Xử lý khi thanh toán thành công (SÀN THU HỘ)
        /// </summary>
        private async Task HandlePaymentSuccessAsync(Transaction transaction, List<Order> orders, PayOSWebhookData webhook)
        {
            _logger.LogInformation("Processing payment SUCCESS for {Count} order(s)", orders.Count);

            // 1. Update Transaction
            transaction.Status = TransactionStatus.Completed;
            transaction.PayOSTransactionId = webhook.Data.Reference ?? webhook.Data.PaymentLinkId;
            transaction.CompletedAt = DateTime.UtcNow;
            transaction.PayOSMetadata = JsonSerializer.Serialize(webhook);

            await _unitOfWork.Transactions.UpdateAsync(transaction);

            // 2. ⭐ Update TẤT CẢ Orders & Chia tiền cho các shops
            var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

            foreach (var order in orders)
            {
                // Update order status
                order.PaymentStatus = PaymentStatus.Paid;
                order.Status = OrderStatus.Processing;

                await _unitOfWork.Orders.UpdateAsync(order);

                // Tính tiền shop nhận được cho order này
                decimal orderPlatformFee = order.Total * config.DefaultCommissionRate / 100;
                decimal shopAmount = order.Total - orderPlatformFee;

                // Cộng tiền vào ShopWallet.PendingBalance
                await _shopWalletService.AddPendingBalanceAsync(
                    order.ShopId,
                    shopAmount,
                    order.Id,
                    $"Doanh thu don hang {order.OrderCode}");

                _logger.LogInformation("  ✅ Order {OrderCode}: Shop {ShopId} receives {Amount:N0} VND (Pending), Fee: {Fee:N0}",
                    order.OrderCode, order.ShopId, shopAmount, orderPlatformFee);
            }

            _logger.LogInformation("✅ Payment SUCCESS: Transaction {TxId}, Total: {Total:N0}, Platform fee: {Fee:N0}",
                transaction.Id, transaction.TotalAmount, transaction.PlatformFeeAmount);

            // TODO: Send email notifications
            // TODO: Send push notifications
        }

        /// <summary>
        /// Xử lý khi thanh toán thất bại
        /// </summary>
        private async Task HandlePaymentFailedAsync(Transaction transaction, List<Order> orders, PayOSWebhookData webhook)
        {
            _logger.LogInformation("Processing payment FAILED for {Count} order(s)", orders.Count);

            // 1. Update Transaction
            transaction.Status = TransactionStatus.Failed;
            transaction.PayOSMetadata = JsonSerializer.Serialize(webhook);

            await _unitOfWork.Transactions.UpdateAsync(transaction);

            // 2. Update tất cả Orders
            foreach (var order in orders)
            {
                order.PaymentStatus = PaymentStatus.Failed;
                order.Status = OrderStatus.Cancelled;

                await _unitOfWork.Orders.UpdateAsync(order);

                _logger.LogInformation("  ❌ Order {OrderCode} cancelled", order.OrderCode);
            }

            _logger.LogInformation("❌ Payment FAILED: Transaction {TxId}, Reason: {Reason}",
                transaction.Id, webhook.Desc);

            // TODO: Restore product stock
            // TODO: Send email notifications
        }

        public async Task<bool> VerifyPayOSSignatureAsync(string webhookData, string signature)
        {
            try
            {
                var checksumKey = _configuration["PayOS:ChecksumKey"];
                var computedSignature = ComputeHmacSha256(webhookData, checksumKey);

                var isValid = computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);

                if (!isValid)
                {
                    _logger.LogWarning("⚠️ PayOS signature verification FAILED");
                    _logger.LogDebug("Received: {Received}", signature);
                    _logger.LogDebug("Computed: {Computed}", computedSignature);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying PayOS signature");
                return false;
            }
        }

        public async Task<Transaction?> GetTransactionStatusAsync(string orderId)
        {
            return await _unitOfWork.Transactions.GetByOrderIdAsync(orderId);
        }

        public async Task<bool> CancelPaymentAsync(string orderId)
        {
            var transaction = await _unitOfWork.Transactions.GetByOrderIdAsync(orderId);

            if (transaction == null || transaction.Status != TransactionStatus.Pending)
            {
                _logger.LogWarning("Cannot cancel: Transaction not found or not pending for Order: {OrderId}", orderId);
                return false;
            }

            transaction.Status = TransactionStatus.Cancelled;
            await _unitOfWork.Transactions.UpdateAsync(transaction);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Payment cancelled for Order: {OrderId}", orderId);

            return true;
        }

        // Helper Methods
        private long GenerateOrderCode(string transactionId)
        {
            var guidPart = transactionId.Replace("-", "");
            var substring = guidPart.Substring(Math.Max(0, guidPart.Length - 10));
            var hash = substring.GetHashCode();
            return Math.Abs(hash) % 9999999999;
        }

        private async Task<Transaction?> FindTransactionByOrderCode(long orderCode)
        {
            return await _unitOfWork.Transactions.GetAsync(t => t.PayOSOrderCode == orderCode);
        }

        private string ComputeHmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }

    // ============================================================
    // DTOs FOR PAYOS API
    // ============================================================

    public class PayOSCreateResponse
    {
        public string Code { get; set; }
        public string Desc { get; set; }
        public PayOSData Data { get; set; }
    }

    public class PayOSData
    {
        public string Bin { get; set; }
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; }
        public long OrderCode { get; set; }
        public string Currency { get; set; }
        public string PaymentLinkId { get; set; }
        public string Status { get; set; }
        public string CheckoutUrl { get; set; }
        public string QrCode { get; set; }
    }

    public class PayOSWebhookData
    {
        public string Code { get; set; }
        public string Desc { get; set; }
        public PayOSWebhookDetail Data { get; set; }
        public string Signature { get; set; }
    }

    public class PayOSWebhookDetail
    {
        public long OrderCode { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public string AccountNumber { get; set; }
        public string Reference { get; set; }
        public string TransactionDateTime { get; set; }
        public string Currency { get; set; }
        public string PaymentLinkId { get; set; }
        public string Code { get; set; }
        public string Desc { get; set; }
        public string CounterAccountBankId { get; set; }
        public string CounterAccountBankName { get; set; }
        public string CounterAccountName { get; set; }
        public string CounterAccountNumber { get; set; }
        public string VirtualAccountName { get; set; }
        public string VirtualAccountNumber { get; set; }
    }
}