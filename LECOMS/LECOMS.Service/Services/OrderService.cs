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
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IUnitOfWork uow,
            IPaymentService paymentService,
            ILogger<OrderService> logger)
        {
            _uow = uow;
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// ⭐ Tạo order từ cart + payment link (SÀN THU HỘ)
        /// 
        /// FLOW:
        /// 1. Tạo NHIỀU ORDERS (1 order/shop)
        /// 2. Tạo 1 TRANSACTION DUY NHẤT cho tất cả orders
        /// 3. Tạo 1 PAYMENT LINK DUY NHẤT
        /// 4. Customer thanh toán 1 lần cho tất cả
        /// </summary>
        public async Task<CheckoutResultDTO> CreateOrderFromCartAsync(string userId, CheckoutRequestDTO checkout)
        {
            _logger.LogInformation("=== START CHECKOUT (SÀN THU HỘ) for User: {UserId} ===", userId);

            using var tx = await _uow.BeginTransactionAsync();
            try
            {
                // 1. Lấy cart với products & shops
                var cart = await _uow.Carts.GetByUserIdAsync(
                    userId,
                    includeProperties: "Items,Items.Product,Items.Product.Shop");

                if (cart == null || !cart.Items.Any())
                {
                    throw new InvalidOperationException("Cart is empty. Please add products before checkout.");
                }

                _logger.LogInformation("✅ Cart found with {Count} items", cart.Items.Count);

                // 2. Group cart items theo ShopId
                var itemsByShop = cart.Items
                    .Where(i => i.Product != null)
                    .GroupBy(i => i.Product.ShopId)
                    .ToList();

                if (itemsByShop.Count == 0)
                {
                    throw new InvalidOperationException("No valid items in cart.");
                }

                _logger.LogInformation("Creating orders for {ShopCount} shop(s)...", itemsByShop.Count);

                var createdOrders = new List<Order>();
                var totalAmount = 0m;

                // 3. ⭐ TẠO NHIỀU ORDERS (1 ORDER/SHOP)
                foreach (var shopGroup in itemsByShop)
                {
                    var shopId = shopGroup.Key;
                    var shopItems = shopGroup.ToList();

                    // Validate stock & tính subtotal
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

                    // Tạo Order cho shop này
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
                        ShippingFee = 0m, // Shipping fee tính chung, không chia theo shop
                        Discount = 0m,
                        Total = subtotal,
                        Status = OrderStatus.Pending,
                        PaymentStatus = PaymentStatus.Pending,
                        BalanceReleased = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _uow.Orders.AddAsync(order);

                    // Tạo OrderDetails
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

                        // Giảm stock
                        item.Product.Stock -= item.Quantity;
                        await _uow.Products.UpdateAsync(item.Product);
                    }

                    createdOrders.Add(order);
                    totalAmount += order.Total;

                    _logger.LogInformation("✅ Order created: {OrderCode} for Shop {ShopId}, Subtotal: {Total:N0} VND",
                        order.OrderCode, shopId, order.Total);
                }

                // Cộng shipping fee và discount vào tổng
                var shippingFee = checkout.ShippingFee ?? 0m;
                var discount = checkout.Discount ?? 0m;
                totalAmount += shippingFee - discount;

                _logger.LogInformation("💰 Total breakdown: Orders={OrderTotal:N0}, Shipping={Ship:N0}, Discount={Disc:N0}, Final={Total:N0}",
                    totalAmount - shippingFee + discount, shippingFee, discount, totalAmount);

                // Save orders
                await _uow.CompleteAsync();

                // 4. ⭐ TẠO 1 TRANSACTION DUY NHẤT cho toàn bộ cart
                _logger.LogInformation("Creating SINGLE transaction for {OrderCount} order(s)...", createdOrders.Count);

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

                    // ⭐ Store ALL order IDs (comma-separated)
                    OrderId = string.Join(",", createdOrders.Select(o => o.Id)),

                    TotalAmount = totalAmount,
                    PlatformFeePercent = config.DefaultCommissionRate,
                    PlatformFeeAmount = platformFeeAmount,
                    ShopAmount = totalShopAmount,
                    Status = TransactionStatus.Pending,
                    PaymentMethod = "PayOS",
                    CreatedAt = DateTime.UtcNow,
                    Note = $"Checkout by {userId} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
                };

                await _uow.Transactions.AddAsync(transaction);
                await _uow.CompleteAsync();

                _logger.LogInformation("✅ Transaction created: {TxId}", transaction.Id);
                _logger.LogInformation("   Total: {Total:N0}, Platform fee: {Fee:N0} ({Rate}%), Shops receive: {Shop:N0}",
                    totalAmount, platformFeeAmount, config.DefaultCommissionRate, totalShopAmount);

                // 5. ⭐ TẠO 1 PAYMENT LINK DUY NHẤT cho transaction
                _logger.LogInformation("Creating payment link for transaction...");

                string paymentUrl;
                try
                {
                    paymentUrl = await _paymentService.CreatePaymentLinkForMultipleOrdersAsync(
                        transaction.Id,
                        createdOrders);

                    _logger.LogInformation("✅ Payment link created: {Url}", paymentUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to create payment link");
                    throw new InvalidOperationException(
                        "Failed to create payment link. Please try again.", ex);
                }

                // 6. Clear cart
                _logger.LogInformation("Clearing cart...");
                foreach (var item in cart.Items.ToList())
                {
                    await _uow.CartItems.DeleteAsync(item);
                }

                await _uow.CompleteAsync();
                await tx.CommitAsync();

                _logger.LogInformation("=== ✅ CHECKOUT SUCCESS: {OrderCount} order(s), 1 payment link, Total: {Total:N0} VND ===",
                    createdOrders.Count, totalAmount);

                // 7. Build result DTO
                var result = new CheckoutResultDTO
                {
                    Orders = createdOrders.Select(o => new OrderDTO
                    {
                        Id = o.Id,
                        OrderCode = o.OrderCode,
                        UserId = o.UserId,
                        ShopId = o.ShopId,
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
                        Details = new List<OrderDetailDTO>()
                    }).ToList(),

                    PaymentUrl = paymentUrl, // ⭐ 1 URL duy nhất

                    TotalAmount = totalAmount
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ❌ CHECKOUT FAILED for User: {UserId} ===", userId);
                await tx.RollbackAsync();
                throw;
            }
        }

        // Other methods...
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
            await _uow.CompleteAsync();

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
            await _uow.CompleteAsync();

            _logger.LogInformation("✅ Order {OrderId} confirmed received by user {UserId}", orderId, userId);

            return MapToDTO(order);
        }

        // Helper methods
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