using LECOMS.Data.DTOs.Order;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    /// <summary>
    /// Service implementation cho Order - SÀN THU HỘ
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly IPaymentService _paymentService;
        private readonly ICustomerWalletService _customerWalletService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IUnitOfWork uow,
            IPaymentService paymentService,
            ICustomerWalletService customerWalletService,
            ILogger<OrderService> logger)
        {
            _uow = uow;
            _paymentService = paymentService;
            _customerWalletService = customerWalletService;
            _logger = logger;
        }

        /// <summary>
        /// ⭐ Tạo order từ cart + payment link (SÀN THU HỘ)
        /// </summary>
        public async Task<CheckoutResultDTO> CreateOrderFromCartAsync(string userId, CheckoutRequestDTO checkout)
        {
            _logger.LogInformation("=== START CHECKOUT for User: {UserId} ===", userId);

            using var tx = await _uow.BeginTransactionAsync();
            try
            {
                // 1. Lấy cart
                var cart = await _uow.Carts.GetByUserIdAsync(
                    userId,
                    includeProperties: "Items,Items.Product,Items.Product.Shop");

                if (cart == null || !cart.Items.Any())
                {
                    throw new InvalidOperationException("Cart is empty.");
                }

                // 2. Lọc sản phẩm (nếu có SelectedProductIds)
                var itemsToCheckout = cart.Items.ToList();

                if (checkout.SelectedProductIds != null && checkout.SelectedProductIds.Any())
                {
                    itemsToCheckout = cart.Items
                        .Where(i => checkout.SelectedProductIds.Contains(i.ProductId))
                        .ToList();

                    if (!itemsToCheckout.Any())
                    {
                        throw new InvalidOperationException("No valid products selected.");
                    }
                }

                // 3. Group theo Shop
                var itemsByShop = itemsToCheckout
                    .Where(i => i.Product != null && i.Product.Shop != null)
                    .GroupBy(i => i.Product.ShopId)
                    .ToList();

                if (!itemsByShop.Any())
                {
                    throw new InvalidOperationException("No valid products in cart.");
                }

                var createdOrders = new List<Order>();
                decimal grandSubtotal = 0m;

                // ============================================================
                // STEP 1: TẠO ORDERS VỚI SUBTOTAL ONLY
                // ============================================================
                foreach (var shopGroup in itemsByShop)
                {
                    var shopId = shopGroup.Key;
                    var shopItems = shopGroup.ToList();

                    decimal subtotal = 0m;

                    foreach (var item in shopItems)
                    {
                        if (item.Product == null)
                        {
                            item.Product = await _uow.Products.GetAsync(p => p.Id == item.ProductId);
                        }

                        if (item.Product == null)
                        {
                            throw new InvalidOperationException($"Product {item.ProductId} not found.");
                        }

                        if (item.Product.Stock < item.Quantity)
                        {
                            throw new InvalidOperationException(
                                $"Insufficient stock for '{item.Product.Name}'. Available: {item.Product.Stock}, Requested: {item.Quantity}");
                        }

                        subtotal += item.Product.Price * item.Quantity;
                    }

                    var orderCode = await GenerateOrderCodeAsync();

                    var order = new Order
                    {
                        Id = Guid.NewGuid().ToString(),
                        OrderCode = orderCode,
                        UserId = userId,
                        ShopId = shopId,
                        ShipToName = checkout.ShipToName,
                        ShipToPhone = checkout.ShipToPhone,
                        ShipToAddress = checkout.ShipToAddress,
                        Subtotal = subtotal,
                        ShippingFee = 0m,
                        Discount = 0m,
                        Total = subtotal,
                        Status = OrderStatus.Pending,
                        PaymentStatus = PaymentStatus.Pending,
                        BalanceReleased = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _uow.Orders.AddAsync(order);

                    foreach (var item in shopItems)
                    {
                        var detail = new OrderDetail
                        {
                            Id = Guid.NewGuid().ToString(),
                            OrderId = order.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.Product.Price
                        };
                        await _uow.OrderDetails.AddAsync(detail);

                        item.Product.Stock -= item.Quantity;
                        await _uow.Products.UpdateAsync(item.Product);
                    }

                    createdOrders.Add(order);
                    grandSubtotal += subtotal;

                    _logger.LogInformation("Created Order {OrderCode}: Subtotal={Subtotal:N0}",
                        orderCode, subtotal);
                }

                //await _uow.CompleteAsync();

                // ============================================================
                // STEP 2: TÍNH SHIPPING FEE & DISCOUNT
                // ============================================================

                decimal totalShippingFee = CalculateShippingFee(
                    checkout.ShipToAddress,
                    itemsByShop.Count,
                    grandSubtotal);

                _logger.LogInformation("🚚 Calculated Shipping Fee: {Fee:N0} VND for {Shops} shop(s)",
                    totalShippingFee, itemsByShop.Count);

                decimal totalDiscount = 0m;
                string? voucherUsed = null;

                if (!string.IsNullOrEmpty(checkout.VoucherCode))
                {
                    var voucherResult = ValidateAndApplyVoucher(
                        checkout.VoucherCode,
                        userId,
                        grandSubtotal);

                    totalDiscount = voucherResult.DiscountAmount;
                    voucherUsed = checkout.VoucherCode;
                }

                _logger.LogInformation("🎁 Applied Discount: {Discount:N0} VND", totalDiscount);

                // ============================================================
                // STEP 3: PHÂN BỔ SHIPPING & DISCOUNT
                // ============================================================

                decimal totalAmount = 0m;
                decimal remainingShipping = totalShippingFee;
                decimal remainingDiscount = totalDiscount;

                for (int i = 0; i < createdOrders.Count; i++)
                {
                    var order = createdOrders[i];

                    decimal orderShippingFee;
                    decimal orderDiscount;

                    if (i == createdOrders.Count - 1)
                    {
                        orderShippingFee = remainingShipping;
                        orderDiscount = remainingDiscount;
                    }
                    else
                    {
                        decimal ratio = order.Subtotal / grandSubtotal;
                        orderShippingFee = Math.Round(totalShippingFee * ratio, 0);
                        orderDiscount = Math.Round(totalDiscount * ratio, 0);

                        remainingShipping -= orderShippingFee;
                        remainingDiscount -= orderDiscount;
                    }

                    order.ShippingFee = orderShippingFee;
                    order.Discount = orderDiscount;
                    order.Total = order.Subtotal + orderShippingFee - orderDiscount;

                   // await _uow.Orders.UpdateAsync(order);

                    totalAmount += order.Total;

                    _logger.LogInformation("Updated Order {OrderCode}: Subtotal={Sub:N0}, Shipping={Ship:N0}, Discount={Disc:N0}, Total={Total:N0}",
                        order.OrderCode, order.Subtotal, order.ShippingFee, order.Discount, order.Total);
                }

               // await _uow.CompleteAsync();

                _logger.LogInformation("💰 GRAND TOTAL:");
                _logger.LogInformation("   Subtotal:  {Sub:N0} VND", grandSubtotal);
                _logger.LogInformation("   Shipping: +{Ship:N0} VND", totalShippingFee);
                _logger.LogInformation("   Discount: -{Disc:N0} VND", totalDiscount);
                _logger.LogInformation("   ─────────────────────");
                _logger.LogInformation("   Total:     {Total:N0} VND", totalAmount);

                // ============================================================
                // STEP 4: XỬ LÝ THANH TOÁN
                // ============================================================

                string? paymentUrl = null;
                decimal walletAmountUsed = 0m;
                decimal payosAmountRequired = totalAmount;

                switch (checkout.PaymentMethod?.ToUpper() ?? "PAYOS")
                {
                    case "WALLET":
                        await ProcessWalletPaymentAsync(userId, totalAmount, createdOrders);
                        payosAmountRequired = 0;
                        walletAmountUsed = totalAmount;
                        break;

                    case "MIXED":
                        var mixedResult = await ProcessMixedPaymentAsync(
                            userId,
                            totalAmount,
                            checkout.WalletAmountToUse,
                            createdOrders);

                        walletAmountUsed = mixedResult.WalletUsed;
                        payosAmountRequired = mixedResult.PayOSRequired;

                        if (payosAmountRequired > 0)
                        {
                            var transaction = await CreateTransactionAsync(createdOrders, payosAmountRequired);
                            paymentUrl = await _paymentService.CreatePaymentLinkForMultipleOrdersAsync(
                                transaction.Id,
                                createdOrders);
                        }
                        break;

                    case "PAYOS":
                    default:
                        var transactionPayOS = await CreateTransactionAsync(createdOrders, totalAmount);
                        paymentUrl = await _paymentService.CreatePaymentLinkForMultipleOrdersAsync(
                            transactionPayOS.Id,
                            createdOrders);
                        break;
                }

                // ============================================================
                // STEP 5: CLEAR CART
                // ============================================================

                _logger.LogInformation("Clearing {Count} items from cart...", itemsToCheckout.Count);
                foreach (var item in itemsToCheckout)
                {
                    await _uow.CartItems.DeleteAsync(item);
                }

                await _uow.CompleteAsync();
                await tx.CommitAsync();

                _logger.LogInformation("=== ✅ CHECKOUT SUCCESS: {OrderCount} order(s), Total: {Total:N0} VND ===",
                    createdOrders.Count, totalAmount);

                return new CheckoutResultDTO
                {
                    Orders = createdOrders.Select(MapToDTO).ToList(),
                    PaymentUrl = paymentUrl,
                    TotalAmount = totalAmount,
                    PaymentMethod = checkout.PaymentMethod ?? "PayOS",
                    WalletAmountUsed = walletAmountUsed,
                    PayOSAmountRequired = payosAmountRequired,
                    DiscountApplied = totalDiscount,
                    ShippingFee = totalShippingFee,
                    VoucherCodeUsed = voucherUsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ❌ CHECKOUT FAILED for User: {UserId} ===", userId);
                await tx.RollbackAsync();
                throw;
            }
        }

        // ============================================================
        // HELPER METHODS - CALCULATION
        // ============================================================

        private decimal CalculateShippingFee(string address, int shopCount, decimal subtotal)
        {
            decimal baseFee = 30000m;
            decimal perShopFee = 10000m;

            decimal totalFee = baseFee + (shopCount - 1) * perShopFee;

            if (subtotal >= 500000)
            {
                totalFee = 0;
                _logger.LogInformation("🎉 Free shipping applied (subtotal >= 500k)");
            }

            return totalFee;
        }

        private (decimal DiscountAmount, string VoucherCode) ValidateAndApplyVoucher(
            string voucherCode,
            string userId,
            decimal totalAmount)
        {
            _logger.LogInformation("Voucher validation: {Code} (Not implemented yet)", voucherCode);
            return (0m, voucherCode);
        }

        // ============================================================
        // HELPER METHODS - PAYMENT
        // ============================================================

        private async Task<Transaction> CreateTransactionAsync(List<Order> orders, decimal totalAmount)
        {
            var config = await _uow.PlatformConfigs.GetConfigAsync();
            if (config == null)
            {
                throw new InvalidOperationException("Platform configuration not found");
            }

            decimal platformFeeAmount = totalAmount * config.DefaultCommissionRate / 100;
            decimal totalShopAmount = totalAmount - platformFeeAmount;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                OrderId = string.Join(",", orders.Select(o => o.Id)),
                TotalAmount = totalAmount,
                PlatformFeePercent = config.DefaultCommissionRate,
                PlatformFeeAmount = platformFeeAmount,
                ShopAmount = totalShopAmount,
                Status = TransactionStatus.Pending,
                PaymentMethod = "PayOS",
                CreatedAt = DateTime.UtcNow,
                Note = $"Checkout by user at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
            };

            await _uow.Transactions.AddAsync(transaction);
            //await _uow.CompleteAsync();

            _logger.LogInformation("✅ Transaction created: {TransactionId}, Amount: {Amount:N0}",
                transaction.Id, totalAmount);

            return transaction;
        }

        private async Task ProcessWalletPaymentAsync(
            string userId,
            decimal amount,
            List<Order> orders)
        {
            _logger.LogInformation("Processing WALLET payment for {Amount:N0} VND", amount);

            var hasBalance = await _customerWalletService.HasSufficientBalanceAsync(userId, amount);
            if (!hasBalance)
            {
                throw new InvalidOperationException("Insufficient wallet balance.");
            }

            await _customerWalletService.DeductBalanceAsync(
                userId,
                amount,
                WalletTransactionType.Payment,
                string.Join(",", orders.Select(o => o.Id)),
                $"Thanh toán đơn hàng {string.Join(", ", orders.Select(o => o.OrderCode))}");

            foreach (var order in orders)
            {
                order.PaymentStatus = PaymentStatus.Paid;
                //await _uow.Orders.UpdateAsync(order);
            }

            var transaction = await CreateTransactionAsync(orders, amount);
            transaction.Status = TransactionStatus.Completed;
            //await _uow.Transactions.UpdateAsync(transaction);

            await DistributeRevenueToShopsAsync(orders, transaction);

            //await _uow.CompleteAsync();

            _logger.LogInformation("✅ Wallet payment completed successfully");
        }

        private async Task<(decimal WalletUsed, decimal PayOSRequired)> ProcessMixedPaymentAsync(
            string userId,
            decimal totalAmount,
            decimal? walletAmountToUse,
            List<Order> orders)
        {
            _logger.LogInformation("Processing MIXED payment: Total {Total:N0}, Wallet request: {Wallet}",
                totalAmount, walletAmountToUse);

            var balance = await _customerWalletService.GetBalanceAsync(userId);

            decimal walletUsed = Math.Min(
                walletAmountToUse ?? balance,
                Math.Min(balance, totalAmount)
            );

            decimal payosRequired = totalAmount - walletUsed;

            _logger.LogInformation("Calculated: Wallet = {Wallet:N0}, PayOS = {PayOS:N0}",
                walletUsed, payosRequired);

            if (walletUsed > 0)
            {
                await _customerWalletService.DeductBalanceAsync(
                    userId,
                    walletUsed,
                    WalletTransactionType.Payment,
                    string.Join(",", orders.Select(o => o.Id)),
                    $"Thanh toán một phần đơn hàng (Wallet: {walletUsed:N0} VND)");

                _logger.LogInformation("✅ Deducted {Amount:N0} from wallet", walletUsed);
            }

            return (walletUsed, payosRequired);
        }

        private async Task DistributeRevenueToShopsAsync(List<Order> orders, Transaction transaction)
        {
            _logger.LogInformation("Distributing revenue to shops...");

            var config = await _uow.PlatformConfigs.GetConfigAsync();
            if (config == null)
            {
                throw new InvalidOperationException("Platform configuration not found");
            }

            var ordersByShop = orders.GroupBy(o => o.ShopId);

            foreach (var shopGroup in ordersByShop)
            {
                var shopId = shopGroup.Key;
                var shopOrders = shopGroup.ToList();

                decimal shopSubtotal = shopOrders.Sum(o => o.Total);
                decimal platformFee = shopSubtotal * config.DefaultCommissionRate / 100;
                decimal shopReceives = shopSubtotal - platformFee;

                var shopWallet = await _uow.ShopWallets.GetByShopIdAsync(shopId);
                if (shopWallet == null)
                {
                    _logger.LogInformation("Creating new ShopWallet for Shop {ShopId}", shopId);

                    shopWallet = new ShopWallet
                    {
                        Id = Guid.NewGuid().ToString(),
                        ShopId = shopId,
                        AvailableBalance = 0,
                        PendingBalance = 0,
                        TotalEarned = 0,
                        TotalWithdrawn = 0,
                        TotalRefunded = 0,
                        CreatedAt = DateTime.UtcNow,
                        LastUpdated = DateTime.UtcNow
                    };
                    await _uow.ShopWallets.AddAsync(shopWallet);
                }

                decimal balanceBefore = shopWallet.PendingBalance;

                shopWallet.PendingBalance += shopReceives;
                shopWallet.TotalEarned += shopReceives;
                shopWallet.LastUpdated = DateTime.UtcNow;

               // await _uow.ShopWallets.UpdateAsync(shopWallet);

                var walletTransaction = new WalletTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    ShopWalletId = shopWallet.Id,
                    Type = WalletTransactionType.OrderRevenue,
                    Amount = shopReceives,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = shopWallet.PendingBalance,
                    BalanceType = "Pending",
                    Description = $"Doanh thu đơn hàng {string.Join(", ", shopOrders.Select(o => o.OrderCode))}",
                    ReferenceId = transaction.Id,
                    ReferenceType = "Transaction",
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.WalletTransactions.AddAsync(walletTransaction);

                _logger.LogInformation(
                    "✅ Shop {ShopId}: Subtotal={Subtotal:N0}, Fee={Fee:N0} ({Rate}%), Receives={Receives:N0}",
                    shopId, shopSubtotal, platformFee, config.DefaultCommissionRate, shopReceives);
            }

            //await _uow.CompleteAsync();
        }

        // ============================================================
        // OTHER METHODS
        // ============================================================

        public async Task<OrderDTO?> GetByIdAsync(string orderId)
        {
            var order = await _uow.Orders.GetAsync(
                o => o.Id == orderId,
                includeProperties: "Details,Details.Product,Shop,User");

            return order == null ? null : MapToDTO(order);
        }

        public async Task<IEnumerable<OrderDTO>> GetByUserAsync(string userId, int pageNumber = 1, int pageSize = 20)
        {
            var orders = await _uow.Orders.GetAllAsync(
                o => o.UserId == userId,
                includeProperties: "Details,Details.Product,Shop");

            return orders
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDTO);
        }

        public async Task<IEnumerable<OrderDTO>> GetByShopAsync(int shopId, int pageNumber = 1, int pageSize = 20)
        {
            var orders = await _uow.Orders.GetAllAsync(
                o => o.ShopId == shopId,
                includeProperties: "Details,Details.Product,User");

            return orders
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDTO);
        }

        public async Task<OrderDTO> UpdateOrderStatusAsync(string orderId, string status, string userId)
        {
            var order = await _uow.Orders.GetAsync(o => o.Id == orderId, includeProperties: "Shop");

            if (order == null)
                throw new InvalidOperationException("Order not found.");

            var shop = await _uow.Shops.GetAsync(s => s.Id == order.ShopId);
            if (shop?.SellerId != userId)
                throw new UnauthorizedAccessException("You are not authorized to update this order.");

            if (!Enum.TryParse<OrderStatus>(status, out var newStatus))
                throw new ArgumentException("Invalid status.");

            order.Status = newStatus;
            await _uow.Orders.UpdateAsync(order);
            //await _uow.CompleteAsync();

            _logger.LogInformation("Order {OrderId} status updated to {Status} by user {UserId}",
                orderId, status, userId);

            return MapToDTO(order);
        }

        public async Task<OrderDTO> ConfirmReceivedAsync(string orderId, string userId)
        {
            var order = await _uow.Orders.GetAsync(o => o.Id == orderId);

            if (order == null)
                throw new InvalidOperationException("Order not found.");

            if (order.UserId != userId)
                throw new UnauthorizedAccessException("You are not authorized to confirm this order.");

            if (order.Status != OrderStatus.Shipping && order.Status != OrderStatus.Processing)
                throw new InvalidOperationException("Order must be in Shipping or Processing status to confirm received.");

            order.Status = OrderStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;

            await _uow.Orders.UpdateAsync(order);
            //await _uow.CompleteAsync();

            _logger.LogInformation("✅ Order {OrderId} confirmed received by user {UserId}", orderId, userId);

            return MapToDTO(order);
        }

        private async Task<string> GenerateOrderCodeAsync()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(100, 999);
            return $"ORD{timestamp}{random}";
        }

        private static OrderDTO MapToDTO(Order o)
        {
            return new OrderDTO
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                UserId = o.UserId,
                ShopId = o.ShopId,
                ShopName = o.Shop?.Name,
                CustomerName = o.User?.UserName,
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
                Details = o.Details?.Select(d => new OrderDetailDTO
                {
                    ProductId = d.ProductId,
                    ProductName = d.Product?.Name ?? string.Empty,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice
                }).ToList() ?? new List<OrderDetailDTO>()
            };
        }
    }
}