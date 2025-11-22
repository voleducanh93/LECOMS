using LECOMS.Data.DTOs.Order;
using LECOMS.Data.DTOs.Voucher;
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
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly IPaymentService _paymentService;
        private readonly ICustomerWalletService _customerWalletService;
        private readonly IShopWalletService _shopWalletService;
        private readonly IVoucherService _voucherService;
        private readonly ILogger<OrderService> _logger;

        private const decimal FIXED_SHIPPING_FEE = 30000m;

        public OrderService(
            IUnitOfWork uow,
            IPaymentService paymentService,
            ICustomerWalletService customerWalletService,
            IShopWalletService shopWalletService,
            IVoucherService voucherService,
            ILogger<OrderService> logger)
        {
            _uow = uow;
            _paymentService = paymentService;
            _customerWalletService = customerWalletService;
            _shopWalletService = shopWalletService;
            _voucherService = voucherService;
            _logger = logger;
        }

        // =====================================================================
        // CHECKOUT (CART)
        // =====================================================================
        public async Task<CheckoutResultDTO> CreateOrderFromCartAsync(string userId, CheckoutRequestDTO checkout)
        {
            using var tx = await _uow.BeginTransactionAsync();

            try
            {
                var cart = await _uow.Carts.GetByUserIdAsync(
                    userId,
                    includeProperties:
                    "Items,Items.Product,Items.Product.Images,Items.Product.Category,Items.Product.Shop");

                if (cart == null || !cart.Items.Any())
                    throw new InvalidOperationException("Cart empty.");

                var selectedItems =
                    (checkout.SelectedProductIds?.Any() ?? false)
                        ? cart.Items.Where(i => checkout.SelectedProductIds.Contains(i.ProductId)).ToList()
                        : cart.Items.ToList();

                if (!selectedItems.Any())
                    throw new InvalidOperationException("No valid products selected.");

                // Group theo shop
                var grouped = selectedItems.GroupBy(i => i.Product.ShopId).ToList();
                var createdOrders = new List<Order>();

                foreach (var group in grouped)
                {
                    var items = group.ToList();
                    decimal subtotal = 0;

                    foreach (var item in items)
                    {
                        if (item.Product.Stock < item.Quantity)
                            throw new InvalidOperationException($"Not enough stock for {item.Product.Name}");

                        subtotal += item.Product.Price * item.Quantity;
                    }

                    var order = new Order
                    {
                        Id = Guid.NewGuid().ToString(),
                        OrderCode = await GenerateOrderCodeAsync(),
                        UserId = userId,
                        ShopId = group.Key,
                        ShipToName = checkout.ShipToName,
                        ShipToPhone = checkout.ShipToPhone,
                        ShipToAddress = checkout.ShipToAddress,

                        Subtotal = subtotal,
                        ShippingFee = FIXED_SHIPPING_FEE,
                        Discount = 0,
                        Total = subtotal + FIXED_SHIPPING_FEE,

                        Status = OrderStatus.Pending,
                        PaymentStatus = PaymentStatus.Pending,
                        BalanceReleased = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _uow.Orders.AddAsync(order);

                    foreach (var item in items)
                    {
                        await _uow.OrderDetails.AddAsync(new OrderDetail
                        {
                            Id = Guid.NewGuid().ToString(),
                            OrderId = order.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.Product.Price
                        });

                        item.Product.Stock -= item.Quantity;
                        await _uow.Products.UpdateAsync(item.Product);
                    }

                    createdOrders.Add(order);
                }

                // ===========================
                // APPLY VOUCHER (nếu có)
                // ===========================
                VoucherApplyResultDTO? voucherResult = null;
                var voucherCode = (checkout.VoucherCode ?? string.Empty).Trim();

                if (!string.IsNullOrWhiteSpace(voucherCode))
                {
                    voucherResult = await _voucherService.ValidateAndPreviewAsync(userId, voucherCode, createdOrders);

                    if (!voucherResult.IsValid)
                    {
                        throw new InvalidOperationException(
                            voucherResult.ErrorMessage ?? "Voucher is not valid.");
                    }

                    // gán Discount cho từng order
                    foreach (var od in createdOrders)
                    {
                        var discount = voucherResult.OrderDiscounts
                            .FirstOrDefault(x => x.OrderId == od.Id)?.DiscountAmount ?? 0m;

                        od.Discount = discount;
                        od.Total = od.Subtotal + od.ShippingFee - od.Discount;
                    }
                }

                decimal totalShipping = createdOrders.Sum(o => o.ShippingFee);
                decimal grandTotal = createdOrders.Sum(o => o.Total);
                decimal totalDiscount = createdOrders.Sum(o => o.Discount);

                string method = (checkout.PaymentMethod ?? "PAYOS").ToUpper();
                string? paymentUrl = null;
                decimal walletUsed = 0;
                decimal payOSRequired = grandTotal;

                // ==================== WALLET ====================
                if (method == "WALLET")
                {
                    bool ok = await _customerWalletService.HasSufficientBalanceAsync(userId, grandTotal);
                    if (!ok)
                        throw new InvalidOperationException("Insufficient wallet balance.");

                    await _customerWalletService.DeductBalanceAsync(
                        userId, grandTotal, WalletTransactionType.Payment,
                        string.Join(",", createdOrders.Select(o => o.Id)),
                        $"Thanh toán đơn hàng {string.Join(",", createdOrders.Select(o => o.OrderCode))}");

                    foreach (var o in createdOrders)
                        o.PaymentStatus = PaymentStatus.Paid;

                    // Tạo transaction WALLET
                    var txObj = await CreateTransactionAsync(
                        createdOrders,
                        grandTotal,
                        "WALLET",
                        string.IsNullOrWhiteSpace(voucherCode) ? null : voucherCode);

                    txObj.Status = TransactionStatus.Completed;

                    await _uow.Transactions.UpdateAsync(txObj);
                    await _uow.CompleteAsync();

                    await DistributeRevenueToShopsAsync(createdOrders, txObj);

                    // Mark voucher used luôn (vì wallet không qua webhook)
                    if (!string.IsNullOrWhiteSpace(voucherCode) && voucherResult != null && voucherResult.IsValid)
                    {
                        await _voucherService.MarkVoucherUsedAsync(
                            userId,
                            voucherCode,
                            createdOrders,
                            $"WALLET-{txObj.Id}");
                    }

                    walletUsed = grandTotal;
                    payOSRequired = 0;
                }
                else
                {
                    // ==================== PAYOS ====================
                    var txObj = await CreateTransactionAsync(
                        createdOrders,
                        grandTotal,
                        "PAYOS",
                        string.IsNullOrWhiteSpace(voucherCode) ? null : voucherCode);

                    paymentUrl = await _paymentService.CreatePaymentLinkForMultipleOrdersAsync(txObj.Id, createdOrders);
                }

                // Xóa cart items
                foreach (var item in selectedItems)
                    await _uow.CartItems.DeleteAsync(item);

                // Load lại User để có FullName => tránh CustomerName = null
                var user = await _uow.Users.GetAsync(u => u.Id == userId);
                foreach (var o in createdOrders)
                    o.User = user;

                await _uow.CompleteAsync();
                await tx.CommitAsync();

                return new CheckoutResultDTO
                {
                    Orders = createdOrders.Select(MapToDTO).ToList(),
                    PaymentUrl = paymentUrl,
                    TotalAmount = grandTotal,
                    ShippingFee = totalShipping,
                    PaymentMethod = method,
                    WalletAmountUsed = walletUsed,
                    PayOSAmountRequired = payOSRequired,
                    DiscountApplied = totalDiscount,
                    VoucherCodeUsed = string.IsNullOrWhiteSpace(voucherCode) ? null : voucherCode
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // =====================================================================
        // QUERIES
        // =====================================================================
        public async Task<OrderDTO?> GetByIdAsync(string orderId)
        {
            var order = await _uow.Orders.GetAsync(
                o => o.Id == orderId,
                includeProperties:
                "Details,Details.Product,Details.Product.Images,Details.Product.Category,Shop,User");

            return order == null ? null : MapToDTO(order);
        }

        public async Task<IEnumerable<OrderDTO>> GetByUserAsync(string userId, int pageNumber = 1, int pageSize = 20)
        {
            var orders = await _uow.Orders.GetAllAsync(
                o => o.UserId == userId,
                includeProperties:
                "Details,Details.Product,Details.Product.Images,Details.Product.Category,Shop,User");

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
                includeProperties:
                "Details,Details.Product,Details.Product.Images,Details.Product.Category,User");

            return orders
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDTO);
        }

        public async Task<OrderDTO> UpdateOrderStatusAsync(string orderId, string status, string userId)
        {
            var order = await _uow.Orders.GetAsync(
                o => o.Id == orderId,
                includeProperties: "Shop,User,Details,Details.Product,Details.Product.Images,Details.Product.Category"
            );

            if (order == null)
                throw new InvalidOperationException("Order not found");

            order.Status = Enum.Parse<OrderStatus>(status);
            await _uow.Orders.UpdateAsync(order);
            await _uow.CompleteAsync();

            return MapToDTO(order);
        }

        public async Task<OrderDTO> ConfirmReceivedAsync(string orderId, string userId)
        {
            var order = await _uow.Orders.GetAsync(
                o => o.Id == orderId,
                includeProperties: "User,Shop")
                ?? throw new InvalidOperationException("Order not found.");

            if (order.UserId != userId)
                throw new InvalidOperationException("You are not allowed to confirm this order.");

            if (order.PaymentStatus != PaymentStatus.Paid)
                throw new InvalidOperationException("Order has not been paid.");

            if (order.Status != OrderStatus.Shipping && order.Status != OrderStatus.Processing)
                throw new InvalidOperationException("Order is not in a receivable state.");

            order.Status = OrderStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;

            await _uow.Orders.UpdateAsync(order);

            // ⭐ UPDATE TRANSACTION (Fix)
            var tx = await _uow.Transactions.GetByOrderIdAsync(orderId);
            if (tx != null)
            {
                tx.Status = TransactionStatus.Completed;
                tx.CompletedAt = DateTime.UtcNow;
                await _uow.Transactions.UpdateAsync(tx);
            }

            await _uow.CompleteAsync();

            return MapToDTO(order);
        }

        // =====================================================================
        // SUPPORT
        // =====================================================================
        private static OrderDTO MapToDTO(Order o)
        {
            return new OrderDTO
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
                    ProductName = d.Product?.Name ?? "",
                    ProductImage = d.Product?.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
                    ProductCategory = d.Product?.Category?.Name,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice
                }).ToList()
            };
        }

        // ⭐ Thêm voucherCode vào transaction
        private async Task<Transaction> CreateTransactionAsync(
            List<Order> orders,
            decimal totalAmount,
            string method,
            string? voucherCode = null)
        {
            var config = await _uow.PlatformConfigs.GetConfigAsync();

            decimal platformFeeAmount = totalAmount * config.DefaultCommissionRate / 100;
            decimal shopAmount = totalAmount - platformFeeAmount;

            var tx = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                OrderId = string.Join(",", orders.Select(o => o.Id)),
                TotalAmount = totalAmount,
                PlatformFeePercent = config.DefaultCommissionRate,
                PlatformFeeAmount = platformFeeAmount,
                ShopAmount = shopAmount,
                Status = TransactionStatus.Pending,
                PaymentMethod = method,
                CreatedAt = DateTime.UtcNow,
                VoucherCode = voucherCode  // ⭐ NEW
            };

            await _uow.Transactions.AddAsync(tx);
            await _uow.CompleteAsync();

            return tx;
        }

        private async Task DistributeRevenueToShopsAsync(List<Order> orders, Transaction tx)
        {
            var config = await _uow.PlatformConfigs.GetConfigAsync();

            var groups = orders.GroupBy(o => o.ShopId);

            foreach (var group in groups)
            {
                decimal subtotal = group.Sum(o => o.Total);
                decimal fee = subtotal * config.DefaultCommissionRate / 100;
                decimal shopReceives = subtotal - fee;

                var orderCodes = string.Join(",", group.Select(o => o.OrderCode));
                var orderIds = string.Join(",", group.Select(o => o.Id));

                await _shopWalletService.AddPendingBalanceAsync(
                    group.Key,
                    shopReceives,
                    orderIds,
                    $"Doanh thu đơn hàng {orderCodes} (Transaction {tx.Id})");
            }
        }

        private async Task<string> GenerateOrderCodeAsync()
        {
            var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var rnd = new Random().Next(100, 999);
            return $"ORD{ts}{rnd}";
        }
    }
}
