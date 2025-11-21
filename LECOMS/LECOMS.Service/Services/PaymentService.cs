using LECOMS.Data.DTOs.Order;
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

        // ============================================================
        // PUBLIC METHODS
        // ============================================================

        /// <summary>
        /// ⭐ Tạo payment link cho 1 order đơn lẻ - WITH RETRY SUPPORT
        /// Use case: Retry payment, admin manual generation
        /// </summary>
        public async Task<string> CreatePaymentLinkAsync(string orderId)
        {
            _logger.LogInformation("=== START CreatePaymentLinkAsync (retry/manual) for Order: {OrderId} ===", orderId);

            // ============================================================
            // 1) CHECK IF THIS ORDER OR ANY ORDER IN TRANSACTION WAS PAID
            // ============================================================

            // Lấy transaction chứa orderId (nếu có)
            var existingTx = await _unitOfWork.Transactions.GetByOrderIdAsync(orderId);

            if (existingTx != null)
            {
                // Nếu transaction đã Completed → cấm retry
                if (existingTx.Status == TransactionStatus.Completed)
                    throw new InvalidOperationException("This order group has already been PAID successfully.");

                // Load toàn bộ orders trong giao dịch
                var allOrderIds = existingTx.OrderId
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var oid in allOrderIds)
                {
                    var order = await _unitOfWork.Orders.GetAsync(o => o.Id == oid);
                    if (order != null && order.PaymentStatus == PaymentStatus.Paid)
                        throw new InvalidOperationException(
                            $"Order group already contains PAID order ({order.OrderCode}). Retry forbidden.");
                }
            }
            else
            {
                // Không có transaction → kiểm tra đơn lẻ
                var order = await _unitOfWork.Orders.GetAsync(o => o.Id == orderId);
                if (order == null)
                    throw new InvalidOperationException("Order not found.");

                if (order.PaymentStatus == PaymentStatus.Paid)
                    throw new InvalidOperationException(
                        $"Order {order.OrderCode} has already been PAID.");
            }

            // ============================================================
            // 2) CASE 1: RETRY PAYMENT (transaction exists)
            // ============================================================
            if (existingTx != null)
            {
                _logger.LogInformation("Retrying PaymentLink for existing transaction {TxId}", existingTx.Id);

                // Load full list of orders within the transaction
                var list = new List<Order>();
                var ids = existingTx.OrderId
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var oid in ids)
                {
                    var o = await _unitOfWork.Orders.GetAsync(
                        x => x.Id == oid,
                        includeProperties: "Details.Product,Shop,User");

                    if (o != null) list.Add(o);
                }

                if (!list.Any())
                    throw new InvalidOperationException("Transaction contains no valid orders.");

                // Reset PayOS data for retry
                existingTx.PayOSOrderCode = null;
                existingTx.PayOSPaymentUrl = null;
                existingTx.PayOSTransactionId = null;
                existingTx.Status = TransactionStatus.Pending;
                existingTx.Note += $" | Retry at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

                await _unitOfWork.Transactions.UpdateAsync(existingTx);
                await _unitOfWork.CompleteAsync();

                // Generate new PayOS link
                var url = await CreatePayOSPaymentAsync(existingTx, list);

                existingTx.PayOSPaymentUrl = url;
                await _unitOfWork.Transactions.UpdateAsync(existingTx);
                await _unitOfWork.CompleteAsync();

                return url;
            }

            // ============================================================
            // 3) CASE 2: NEW MANUAL PAYMENT LINK FOR A SINGLE ORDER
            // ============================================================

            _logger.LogInformation("No existing transaction → create NEW single-order transaction.");

            var singleOrder = await _unitOfWork.Orders.GetAsync(
                o => o.Id == orderId,
                includeProperties: "Details.Product,Shop,User");

            if (singleOrder == null)
                throw new InvalidOperationException("Order not found.");

            var cfg = await _unitOfWork.PlatformConfigs.GetConfigAsync()
                      ?? throw new InvalidOperationException("Platform config missing.");

            decimal total = singleOrder.Total;
            decimal fee = total * cfg.DefaultCommissionRate / 100;
            decimal shopAmount = total - fee;

            var newTx = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                OrderId = orderId,
                TotalAmount = total,
                PlatformFeePercent = cfg.DefaultCommissionRate,
                PlatformFeeAmount = fee,
                ShopAmount = shopAmount,
                Status = TransactionStatus.Pending,
                PaymentMethod = "PAYOS",
                CreatedAt = DateTime.UtcNow,
                Note = $"Manual payment for {singleOrder.OrderCode}"
            };

            await _unitOfWork.Transactions.AddAsync(newTx);
            await _unitOfWork.CompleteAsync();

            var paymentUrlNew = await CreatePayOSPaymentAsync(newTx, new List<Order> { singleOrder });

            newTx.PayOSPaymentUrl = paymentUrlNew;
            await _unitOfWork.Transactions.UpdateAsync(newTx);
            await _unitOfWork.CompleteAsync();

            return paymentUrlNew;
        }


        /// <summary>
        /// ⭐ Tạo payment link cho NHIỀU orders (checkout flow)
        /// Use case: Normal checkout from cart
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
                        _logger.LogInformation("  - Order {OrderCode}: Subtotal={Sub:N0}, Shipping={Ship:N0}, Discount={Disc:N0}, Total={Total:N0}",
                            order.OrderCode, order.Subtotal, order.ShippingFee, order.Discount, order.Total);
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

        // ============================================================
        // PRIVATE METHODS
        // ============================================================

        /// <summary>
        /// ⭐⭐⭐ Core method: Call PayOS API - WITH SHIPPING & DISCOUNT ⭐⭐⭐
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

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(checksumKey))
                {
                    throw new InvalidOperationException("PayOS configuration is missing");
                }

                // ⭐ Generate UNIQUE OrderCode with timestamp to avoid error 231
                long orderCode = GenerateUniqueOrderCode();
                transaction.PayOSOrderCode = orderCode;

                _logger.LogInformation("🔢 Generated PayOS OrderCode: {OrderCode}", orderCode);

                // ⭐⭐⭐ BUILD ITEMS ARRAY - INCLUDING SHIPPING & DISCOUNT ⭐⭐⭐
                var items = new List<object>();
                decimal totalShippingFee = 0m;
                decimal totalDiscount = 0m;
                decimal totalProductAmount = 0m;

                // 1. Add all product items from all orders
                foreach (var order in orders)
                {
                    // Ensure details are loaded
                    if (order.Details == null || !order.Details.Any())
                    {
                        var fullOrder = await _unitOfWork.Orders.GetAsync(
                            o => o.Id == order.Id,
                            includeProperties: "Details.Product");

                        if (fullOrder?.Details != null)
                        {
                            order.Details = fullOrder.Details;
                        }
                    }

                    // Add product items
                    if (order.Details != null)
                    {
                        foreach (var detail in order.Details)
                        {
                            var productName = detail.Product?.Name ?? "Product";
                            var productPrice = (int)detail.UnitPrice;

                            items.Add(new
                            {
                                name = productName,
                                quantity = detail.Quantity,
                                price = productPrice
                            });

                            totalProductAmount += detail.UnitPrice * detail.Quantity;

                            _logger.LogInformation("  📦 Product: {Name} x{Qty} = {Price:N0} VND",
                                productName, detail.Quantity, productPrice * detail.Quantity);
                        }
                    }

                    // Accumulate shipping & discount from each order
                    totalShippingFee += order.ShippingFee;
                    totalDiscount += order.Discount;
                }

                // ⭐ 2. ADD SHIPPING FEE AS SEPARATE ITEM (nếu > 0)
                if (totalShippingFee > 0)
                {
                    items.Add(new
                    {
                        name = "Phí vận chuyển",
                        quantity = 1,
                        price = (int)totalShippingFee
                    });

                    _logger.LogInformation("  🚚 Shipping Fee: +{Fee:N0} VND", totalShippingFee);
                }

                // ⭐ 3. ADD DISCOUNT AS NEGATIVE ITEM (nếu > 0)
                if (totalDiscount > 0)
                {
                    items.Add(new
                    {
                        name = "Giảm giá",
                        quantity = 1,
                        price = -(int)totalDiscount  // ⭐ NEGATIVE = discount
                    });

                    _logger.LogInformation("  🎁 Discount: -{Discount:N0} VND", totalDiscount);
                }

                // Fallback if no items (should not happen)
                if (!items.Any())
                {
                    _logger.LogWarning("⚠️ No items found, using fallback");
                    items.Add(new
                    {
                        name = "Đơn hàng",
                        quantity = 1,
                        price = (int)transaction.TotalAmount
                    });
                }

                // Verify total calculation
                var calculatedTotal = totalProductAmount + totalShippingFee - totalDiscount;
                _logger.LogInformation("💰 Total Breakdown:");
                _logger.LogInformation("   Products:  {Prod:N0} VND", totalProductAmount);
                _logger.LogInformation("   Shipping: +{Ship:N0} VND", totalShippingFee);
                _logger.LogInformation("   Discount: -{Disc:N0} VND", totalDiscount);
                _logger.LogInformation("   ─────────────────────────");
                _logger.LogInformation("   Total:     {Total:N0} VND", calculatedTotal);

                if (Math.Abs(calculatedTotal - transaction.TotalAmount) > 1)
                {
                    _logger.LogWarning("⚠️ Total mismatch: Calculated={Calc:N0} vs Transaction={Tx:N0}",
                        calculatedTotal, transaction.TotalAmount);
                }

                // Create description (max 25 chars for PayOS)
                var description = orders.Count == 1
                    ? $"Order {orders[0].OrderCode}"
                    : $"Orders ({orders.Count})";

                if (description.Length > 25)
                {
                    description = description.Substring(0, 25);
                }

                _logger.LogInformation("📝 Description: '{Desc}', Items count: {Count}", description, items.Count);

                // Build payment request payload
                var paymentData = new
                {
                    orderCode = orderCode,
                    amount = (int)transaction.TotalAmount,
                    description = description,
                    returnUrl = _configuration["PayOS:ReturnUrl"],
                    cancelUrl = _configuration["PayOS:CancelUrl"],
                    items = items
                };

                _logger.LogInformation("PayOS Request: OrderCode={OrderCode}, Amount={Amount:N0}, Items={ItemCount}",
                    orderCode, transaction.TotalAmount, items.Count);

                // Calculate signature
                string dataToSign = $"amount={paymentData.amount}&cancelUrl={paymentData.cancelUrl}&description={paymentData.description}&orderCode={paymentData.orderCode}&returnUrl={paymentData.returnUrl}";
                string signature = ComputeHmacSha256(dataToSign, checksumKey);

                var requestPayload = new
                {
                    paymentData.orderCode,
                    paymentData.amount,
                    paymentData.description,
                    paymentData.returnUrl,
                    paymentData.cancelUrl,
                    signature = signature,
                    items = paymentData.items
                };

                var jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("🚀 Calling PayOS API: {Url}", "https://api-merchant.payos.vn/v2/payment-requests");
                _logger.LogDebug("Request payload: {Payload}", jsonPayload);

                // Call PayOS API
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("https://api-merchant.payos.vn/v2/payment-requests", content);

                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("PayOS Response Status: {Status}", response.StatusCode);
                _logger.LogDebug("Response body: {Body}", responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ PayOS API Error: {Status} - {Body}", response.StatusCode, responseBody);
                    throw new Exception($"PayOS API returned {response.StatusCode}: {responseBody}");
                }

                // Deserialize response with proper options
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                PayOSCreateResponse payosResponse;
                try
                {
                    payosResponse = JsonSerializer.Deserialize<PayOSCreateResponse>(responseBody, options);
                    _logger.LogInformation("Deserialize result: {Success}", payosResponse != null ? "Success" : "Null");
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "❌ Failed to deserialize PayOS response");
                    _logger.LogError("Raw response: {Body}", responseBody);
                    throw new Exception($"Invalid PayOS response format: {jsonEx.Message}");
                }

                if (payosResponse == null)
                {
                    _logger.LogError("❌ PayOS response deserialized to null");
                    throw new Exception("PayOS response is null");
                }

                // Check response code
                if (payosResponse.Code != "00")
                {
                    _logger.LogError("❌ PayOS Error: Code={Code}, Desc={Desc}",
                        payosResponse.Code, payosResponse.Desc);
                    throw new Exception($"PayOS error {payosResponse.Code}: {payosResponse.Desc}");
                }

                if (payosResponse.Data == null || string.IsNullOrEmpty(payosResponse.Data.CheckoutUrl))
                {
                    _logger.LogError("❌ PayOS did not return checkout URL");
                    throw new Exception("CheckoutUrl is null or empty");
                }

                string paymentUrl = payosResponse.Data.CheckoutUrl;

                _logger.LogInformation("✅ PayOS payment URL created: {Url}", paymentUrl);

                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in CreatePayOSPaymentAsync");
                throw;
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

            // 2. Update all Orders & Distribute revenue to shops
            var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

            foreach (var order in orders)
            {
                // Update order status
                order.PaymentStatus = PaymentStatus.Paid;
                order.Status = OrderStatus.Processing;

                await _unitOfWork.Orders.UpdateAsync(order);

                // Calculate shop revenue for this order
                decimal orderPlatformFee = order.Total * config.DefaultCommissionRate / 100;
                decimal shopAmount = order.Total - orderPlatformFee;

                // Add to ShopWallet.PendingBalance
                await _shopWalletService.AddPendingBalanceAsync(
                    order.ShopId,
                    shopAmount,
                    order.Id,
                    $"Doanh thu don hang {order.OrderCode}");

                _logger.LogInformation("  ✅ Order {OrderCode}: Shop {ShopId} receives {Amount:N0} VND (Pending), Platform fee: {Fee:N0}",
                    order.OrderCode, order.ShopId, shopAmount, orderPlatformFee);
            }

            _logger.LogInformation("✅ Payment SUCCESS: Transaction {TxId}, Total: {Total:N0} VND, Platform fee: {Fee:N0} VND",
                transaction.Id, transaction.TotalAmount, transaction.PlatformFeeAmount);

            // TODO: Send email notifications to customer & shops
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

            // 2. Update all Orders to Failed/Cancelled
            foreach (var order in orders)
            {
                order.PaymentStatus = PaymentStatus.Failed;
                order.Status = OrderStatus.Cancelled;

                await _unitOfWork.Orders.UpdateAsync(order);

                _logger.LogInformation("  ❌ Order {OrderCode} cancelled due to payment failure", order.OrderCode);
            }

            _logger.LogInformation("❌ Payment FAILED: Transaction {TxId}, Reason: {Reason}",
                transaction.Id, webhook.Desc);

            // TODO: Restore product stock
            // TODO: Send email notification
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        /// <summary>
        /// ⭐ Generate UNIQUE OrderCode với timestamp - FIX ERROR 231
        /// Format: {timestamp-last-7-digits}{random-3-digits}
        /// Example: 5514991777 (timestamp part + random)
        /// </summary>
        private long GenerateUniqueOrderCode()
        {
            // Sử dụng timestamp (seconds) + random để đảm bảo unique mỗi lần
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = new Random().Next(100, 999);

            // OrderCode = last 7 digits of timestamp + 3 random digits
            // This ensures uniqueness even for rapid retry attempts
            var orderCode = (timestamp % 10000000) * 1000 + random;

            _logger.LogInformation("Generated OrderCode: {Code} (Timestamp: {Time}, Random: {Rand})",
                orderCode, timestamp, random);

            return orderCode;
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

        /// <summary>
        /// ⭐ Trả FULL CheckoutResultDTO cho API create-payment-link
        /// Giống y hệt Checkout, nhưng áp dụng cho các order đã tạo trước đó
        /// </summary>
        public async Task<CheckoutResultDTO> CreatePaymentResultForExistingOrdersAsync(string orderId)
        {
            // Tìm transaction theo orderId
            var tx = await _unitOfWork.Transactions.GetByOrderIdAsync(orderId);
            if (tx == null)
                throw new InvalidOperationException("Transaction not found for this order.");

            // Lấy list order IDs
            var orderIds = tx.OrderId.Split(',', StringSplitOptions.RemoveEmptyEntries);

            var orders = new List<Order>();
            foreach (var oid in orderIds)
            {
                var order = await _unitOfWork.Orders.GetAsync(
                    o => o.Id == oid.Trim(),
                    includeProperties:
                    "Details.Product,Details.Product.Images,Details.Product.Category,Shop,User");

                if (order != null)
                    orders.Add(order);
            }

            if (!orders.Any())
                throw new InvalidOperationException("No valid orders found.");

            // Tạo lại link PayOS cho FULL bộ orders
            var paymentUrl = await CreatePaymentLinkForMultipleOrdersAsync(tx.Id, orders);

            // Tính tổng tiền
            decimal totalAmount = orders.Sum(o => o.Total);
            decimal shippingFee = orders.Sum(o => o.ShippingFee);
            decimal discount = orders.Sum(o => o.Discount);

            // Map order DTO
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
                BalanceReleased = o.BalanceReleased,
                CreatedAt = o.CreatedAt,
                CompletedAt = o.CompletedAt,
                Details = o.Details.Select(d => new OrderDetailDTO
                {
                    Id = d.Id,
                    ProductId = d.ProductId,
                    ProductName = d.Product?.Name,
                    ProductImage = d.Product?.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
                    ProductCategory = d.Product?.Category?.Name,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice
                }).ToList()
            }).ToList();

            // ⭐ Trả về FULL object giống y API Checkout
            return new CheckoutResultDTO
            {
                Orders = orderDtos,
                PaymentUrl = paymentUrl,
                TotalAmount = totalAmount,
                PaymentMethod = "PAYOS",
                WalletAmountUsed = 0,                 // retry không dùng wallet
                PayOSAmountRequired = totalAmount,
                DiscountApplied = discount,
                ShippingFee = shippingFee,
                VoucherCodeUsed = null
            };
        }

    }

    // ============================================================
    // DTOs FOR PAYOS API
    // ============================================================

    /// <summary>
    /// Response khi tạo payment link từ PayOS
    /// </summary>
    public class PayOSCreateResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
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
        public string CheckoutUrl { get; set; } = string.Empty;
        public string? QrCode { get; set; }
    }

    /// <summary>
    /// Webhook data từ PayOS khi thanh toán complete
    /// </summary>
    public class PayOSWebhookData
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
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