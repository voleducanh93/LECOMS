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

        // =====================================================================
        // CHECKOUT
        // =====================================================================
        public async Task<CheckoutResultDTO> CreateOrderFromCartAsync(string userId, CheckoutRequestDTO checkout)
        {
            using var tx = await _uow.BeginTransactionAsync();

            try
            {
                // GET CART + FULL PRODUCT INCLUDE
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

                // Group by shop
                var grouped = selectedItems
                    .GroupBy(i => i.Product.ShopId)
                    .ToList();

                var createdOrders = new List<Order>();
                decimal grandSubtotal = 0m;

                // ================== Tạo Order cho từng shop ==================
                foreach (var group in grouped)
                {
                    var shopId = group.Key;
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
                        ShopId = shopId,
                        ShipToName = checkout.ShipToName,
                        ShipToPhone = checkout.ShipToPhone,
                        ShipToAddress = checkout.ShipToAddress,
                        Subtotal = subtotal,
                        ShippingFee = 0,
                        Discount = 0,
                        Total = subtotal,
                        Status = OrderStatus.Pending,
                        PaymentStatus = PaymentStatus.Pending,
                        BalanceReleased = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _uow.Orders.AddAsync(order);

                    foreach (var item in items)
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

                        // Update stock
                        item.Product.Stock -= item.Quantity;
                        await _uow.Products.UpdateAsync(item.Product);
                    }

                    createdOrders.Add(order);
                    grandSubtotal += subtotal;
                }

                // ================== Shipping & Discount ==================
                decimal shippingFee = CalculateShippingFee(
                    checkout.ShipToAddress,
                    createdOrders.Count,
                    grandSubtotal);

                decimal discount = 0m;
                string? voucherUsed = checkout.VoucherCode;

                decimal remainingShipping = shippingFee;
                decimal remainingDiscount = discount;
                decimal grandTotal = 0m;

                foreach (var order in createdOrders)
                {
                    decimal ratio = (grandSubtotal == 0)
                        ? 1
                        : order.Subtotal / grandSubtotal;

                    decimal shipAlloc = Math.Round(shippingFee * ratio, 0);
                    decimal discAlloc = Math.Round(discount * ratio, 0);

                    remainingShipping -= shipAlloc;
                    remainingDiscount -= discAlloc;

                    if (order == createdOrders.Last())
                    {
                        shipAlloc += remainingShipping;
                        discAlloc += remainingDiscount;
                    }

                    order.ShippingFee = shipAlloc;
                    order.Discount = discAlloc;
                    order.Total = order.Subtotal + shipAlloc - discAlloc;

                    grandTotal += order.Total;
                }

                // ================== Payment ==================
                string? paymentUrl = null;
                decimal walletUsed = 0;
                decimal payOSRequired = grandTotal;

                switch ((checkout.PaymentMethod ?? "PAYOS").ToUpper())
                {
                    case "WALLET":
                        await ProcessWalletPaymentAsync(userId, grandTotal, createdOrders);
                        walletUsed = grandTotal;
                        payOSRequired = 0;
                        break;

                    case "MIXED":
                        var mixed = await ProcessMixedPaymentAsync(
                            userId, grandTotal, checkout.WalletAmountToUse, createdOrders);

                        walletUsed = mixed.WalletUsed;
                        payOSRequired = mixed.PayOSRequired;

                        if (payOSRequired > 0)
                        {
                            var txObj = await CreateTransactionAsync(createdOrders, payOSRequired);
                            paymentUrl =
                                await _paymentService.CreatePaymentLinkForMultipleOrdersAsync(
                                    txObj.Id, createdOrders);
                        }
                        break;

                    default: // PAYOS
                        var t = await CreateTransactionAsync(createdOrders, grandTotal);
                        paymentUrl =
                            await _paymentService.CreatePaymentLinkForMultipleOrdersAsync(t.Id, createdOrders);
                        break;
                }

                // CLEAR CART
                foreach (var item in selectedItems)
                    await _uow.CartItems.DeleteAsync(item);

                await _uow.CompleteAsync();
                await tx.CommitAsync();

                return new CheckoutResultDTO
                {
                    Orders = createdOrders.Select(MapToDTO).ToList(),
                    PaymentUrl = paymentUrl,
                    TotalAmount = grandTotal,
                    PaymentMethod = checkout.PaymentMethod ?? "PAYOS",
                    WalletAmountUsed = walletUsed,
                    PayOSAmountRequired = payOSRequired,
                    DiscountApplied = discount,
                    ShippingFee = shippingFee,
                    VoucherCodeUsed = voucherUsed
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // =====================================================================
        // SUPPORT METHODS
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
                    ProductName = d.Product?.Name,
                    ProductImage = d.Product?.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
                    ProductCategory = d.Product?.Category?.Name,
                    //ProductSku = null, // Product không có SKU
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice
                }).ToList()
            };
        }

        private decimal CalculateShippingFee(string address, int shopCount, decimal subtotal)
        {
            if (subtotal >= 500000) return 0;
            return 30000m + (shopCount - 1) * 10000m;
        }

        // ================== WALLET PAYMENT ==================
        private async Task ProcessWalletPaymentAsync(
            string userId, decimal amount, List<Order> orders)
        {
            var ok = await _customerWalletService.HasSufficientBalanceAsync(userId, amount);
            if (!ok) throw new InvalidOperationException("Insufficient wallet balance.");

            await _customerWalletService.DeductBalanceAsync(
                userId, amount, WalletTransactionType.Payment,
                string.Join(",", orders.Select(o => o.Id)),
                $"Thanh toán đơn hàng {string.Join(",", orders.Select(o => o.OrderCode))}");

            foreach (var o in orders)
                o.PaymentStatus = PaymentStatus.Paid;

            var tx = await CreateTransactionAsync(orders, amount);
            tx.Status = TransactionStatus.Completed;

            await DistributeRevenueToShopsAsync(orders, tx);
        }

        private async Task<(decimal WalletUsed, decimal PayOSRequired)>
            ProcessMixedPaymentAsync(
                string userId, decimal total, decimal? walletUse, List<Order> orders)
        {
            var balance = await _customerWalletService.GetBalanceAsync(userId);

            decimal walletUsed = Math.Min(walletUse ?? balance, Math.Min(balance, total));
            decimal payOSRequired = total - walletUsed;

            if (walletUsed > 0)
            {
                await _customerWalletService.DeductBalanceAsync(
                    userId, walletUsed, WalletTransactionType.Payment,
                    string.Join(",", orders.Select(o => o.Id)),
                    $"Thanh toán một phần đơn hàng (Wallet: {walletUsed:N0})");
            }

            return (walletUsed, payOSRequired);
        }

        private async Task<Transaction> CreateTransactionAsync(List<Order> orders, decimal totalAmount)
        {
            var config = await _uow.PlatformConfigs.GetConfigAsync();

            decimal platformFeeAmount = totalAmount *
                config.DefaultCommissionRate / 100;

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
                PaymentMethod = "PAYOS",
                CreatedAt = DateTime.UtcNow
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

                var wallet = await _uow.ShopWallets.GetByShopIdAsync(group.Key);

                if (wallet == null)
                {
                    wallet = new ShopWallet
                    {
                        Id = Guid.NewGuid().ToString(),
                        ShopId = group.Key,
                        AvailableBalance = 0,
                        PendingBalance = 0,
                        TotalEarned = 0,
                        TotalWithdrawn = 0,
                        TotalRefunded = 0,
                        CreatedAt = DateTime.UtcNow,
                        LastUpdated = DateTime.UtcNow
                    };

                    await _uow.ShopWallets.AddAsync(wallet);
                }

                decimal before = wallet.PendingBalance;

                wallet.PendingBalance += shopReceives;
                wallet.TotalEarned += shopReceives;
                wallet.LastUpdated = DateTime.UtcNow;

                await _uow.ShopWallets.UpdateAsync(wallet);

                var wt = new WalletTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    ShopWalletId = wallet.Id,
                    Type = WalletTransactionType.OrderRevenue,
                    Amount = shopReceives,
                    BalanceBefore = before,
                    BalanceAfter = wallet.PendingBalance,
                    BalanceType = "Pending",
                    Description = $"Doanh thu đơn hàng {string.Join(",", group.Select(o => o.OrderCode))}",
                    ReferenceId = tx.Id,
                    ReferenceType = "Transaction",
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.WalletTransactions.AddAsync(wt);
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
            var order = await _uow.Orders.GetAsync(o => o.Id == orderId)
                ?? throw new InvalidOperationException("Order not found");

            order.Status = Enum.Parse<OrderStatus>(status);
            await _uow.Orders.UpdateAsync(order);

            return MapToDTO(order);
        }

        public async Task<OrderDTO> ConfirmReceivedAsync(string orderId, string userId)
        {
            var order = await _uow.Orders.GetAsync(o => o.Id == orderId)
                ?? throw new InvalidOperationException("Order not found");

            order.Status = OrderStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;

            await _uow.Orders.UpdateAsync(order);

            return MapToDTO(order);
        }

        // =====================================================================
        // ORDER CODE GEN
        // =====================================================================
        private async Task<string> GenerateOrderCodeAsync()
        {
            var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var rnd = new Random().Next(100, 999);
            return $"ORD{ts}{rnd}";
        }
    }
}
