using LECOMS.Data.DTOs.Gamification;
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
        private readonly IPlatformWalletService _platformWalletService;
        private readonly ILogger<OrderService> _logger;
        private readonly IGamificationService _gamification; // ⭐ thêm
        private readonly INotificationService _notification;
        private readonly IAchievementService _achievement;
        private readonly IShippingService _shippingService;  // ⭐ THÊM
        private const decimal FIXED_SHIPPING_FEE = 30000m;

        public OrderService(
            IUnitOfWork uow,
            IPaymentService paymentService,
            ICustomerWalletService customerWalletService,
            IShopWalletService shopWalletService,
            IVoucherService voucherService,
            ILogger<OrderService> logger,
            IPlatformWalletService platformWalletService,
            IGamificationService gamification,
            INotificationService notification,
            IAchievementService achievement,
            IShippingService shippingService)
        {
            _uow = uow;
            _paymentService = paymentService;
            _customerWalletService = customerWalletService;
            _shopWalletService = shopWalletService;
            _voucherService = voucherService;
            _logger = logger;
            _platformWalletService = platformWalletService;
            _gamification = gamification;
            _notification = notification;
            _achievement = achievement;
            _shippingService = shippingService;
        }

        // =====================================================================
        // CHECKOUT (CART)
        // =====================================================================
        public async Task<CheckoutResultDTO> CreateOrderFromCartAsync(string userId, CheckoutRequestDTO checkout)
        {
            using var tx = await _uow.BeginTransactionAsync();

            try
            {
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                // STEP 1: Validate địa chỉ giao hàng (GHN format)
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                if (checkout.ToDistrictId <= 0)
                {
                    throw new InvalidOperationException(
                        "Vui lòng chọn Quận/Huyện giao hàng.");
                }

                if (string.IsNullOrWhiteSpace(checkout.ToWardCode))
                {
                    throw new InvalidOperationException(
                        "Vui lòng chọn Phường/Xã giao hàng.");
                }

                _logger.LogInformation(
                    "🛒 Starting checkout for user {UserId} to District={District}, Ward={Ward}",
                    userId, checkout.ToDistrictId, checkout.ToWardCode);

                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                // STEP 2: Load giỏ hàng
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                var cart = await _uow.Carts.GetByUserIdAsync(
                    userId,
                    includeProperties:
                    "Items,Items.Product,Items.Product.Images,Items.Product.Category,Items.Product.Shop");

                if (cart == null || !cart.Items.Any())
                    throw new InvalidOperationException("Giỏ hàng trống.");

                var selectedItems =
                    (checkout.SelectedProductIds?.Any() ?? false)
                        ? cart.Items.Where(i => checkout.SelectedProductIds.Contains(i.ProductId)).ToList()
                        : cart.Items.ToList();

                if (!selectedItems.Any())
                    throw new InvalidOperationException("Không có sản phẩm nào được chọn.");

                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                // STEP 3: Validate không mua hàng từ shop của chính mình
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                var userShopIds = selectedItems
                   .Select(i => i.Product.ShopId)
                   .Distinct()
                   .ToList();

                foreach (var shopId in userShopIds)
                {
                    var shop = await _uow.Shops.GetAsync(s => s.Id == shopId);
                    if (shop != null && shop.SellerId == userId)
                    {
                        throw new InvalidOperationException(
                            $"Bạn không thể mua sản phẩm từ shop '{shop.Name}' của chính mình.");
                    }
                }

                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                // STEP 4: Group theo shop và tạo orders
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                var grouped = selectedItems.GroupBy(i => i.Product.ShopId).ToList();
                var createdOrders = new List<Order>();

                _logger.LogInformation(
                    "📦 Creating orders for {ShopCount} shops with {ProductCount} products",
                    grouped.Count, selectedItems.Count);

                foreach (var group in grouped)
                {
                    int shopId = group.Key;
                    var items = group.ToList();
                    var shopName = items.First().Product.Shop?.Name ?? $"Shop {shopId}";

                    var shop = await _uow.Shops.GetAsync(s => s.Id == shopId)
                         ?? throw new InvalidOperationException($"Shop {shopId} không tồn tại.");

                    if (!shop.IsGHNConnected)
                    {
                        throw new InvalidOperationException(
                            $"Shop '{shop.Name}' chưa kết nối GHN.");
                    }

                    _logger.LogInformation("🏪 Processing shop {ShopId} ({ShopName})", shopId, shopName);

                    // ─────────────────────────────────────────────────
                    // 4.1: Tính subtotal và tổng trọng lượng
                    // ─────────────────────────────────────────────────
                    decimal subtotal = 0;
                    int totalWeight = 0;
                    int? maxLength = null;
                    int? maxWidth = null;
                    int? maxHeight = null;

                    foreach (var item in items)
                    {
                        // Validate stock
                        if (item.Product.Stock < item.Quantity)
                        {
                            throw new InvalidOperationException(
                                $"Sản phẩm '{item.Product.Name}' không đủ hàng. " +
                                $"Còn {item.Product.Stock}, bạn đặt {item.Quantity}.");
                        }

                        subtotal += item.Product.Price * item.Quantity;

                        // ⭐ Tính trọng lượng (nếu Product có Weight, không thì default 500g)
                        int productWeight = item.Product.Weight > 0
                            ? item.Product.Weight
                            : 500;
                        totalWeight += productWeight * item.Quantity;

                        // ⭐ Lấy kích thước lớn nhất (nếu có)
                        if (item.Product.Length.HasValue && item.Product.Length.Value > (maxLength ?? 0))
                            maxLength = item.Product.Length;
                        if (item.Product.Width.HasValue && item.Product.Width.Value > (maxWidth ?? 0))
                            maxWidth = item.Product.Width;
                        if (item.Product.Height.HasValue && item.Product.Height.Value > (maxHeight ?? 0))
                            maxHeight = item.Product.Height;
                    }

                    _logger.LogInformation(
                        "💰 Shop {ShopId}:  Subtotal={Subtotal}đ, Weight={Weight}g",
                        shopId, subtotal, totalWeight);

                    // ─────────────────────────────────────────────────
                    // 4.2: Lấy địa chỉ kho của shop
                    // ─────────────────────────────────────────────────
                    var shopAddress = await _uow.ShopAddresses.GetAsync(
                        sa => sa.ShopId == shopId && sa.IsDefault,
                        includeProperties: "Shop");

                    if (shopAddress == null)
                    {
                        _logger.LogWarning(
                            "⚠️ Shop {ShopId} ({ShopName}) chưa thiết lập địa chỉ kho",
                            shopId, shopName);

                        throw new InvalidOperationException(
                            $"Shop '{shopName}' chưa thiết lập địa chỉ kho. " +
                            $"Vui lòng liên hệ shop hoặc chọn sản phẩm khác.");
                    }

                    // ─────────────────────────────────────────────────
                    // 4. 3:  🚚 GỌI GHN API TÍNH PHÍ SHIP (BẮT BUỘC)
                    // ─────────────────────────────────────────────────
                    decimal shippingFee;
                    string estimatedDeliveryText;

                    try
                    {
                        var shippingResult = await _shippingService.GetShippingDetailsAsync(
                            ghnToken: shop.GHNToken!,
                            ghnShopId: shop.GHNShopId!,
                            fromDistrictId: shopAddress.DistrictId,
                            fromWardCode: shopAddress.WardCode,
                            toDistrictId: checkout.ToDistrictId,
                            toWardCode: checkout.ToWardCode,
                            weight: totalWeight,
                            orderValue: subtotal,
                            serviceTypeId: checkout.ServiceTypeId,
                            length: maxLength,
                            width: maxWidth,
                            height: maxHeight
                        );

                        if (shippingResult == null)
                        {
                            _logger.LogError(
                                "❌ GHN API returned null for Shop {ShopId} ({ShopName}). " +
                                "Cannot calculate shipping fee.",
                                shopId, shopName);

                            throw new InvalidOperationException(
                                $"Không thể tính phí vận chuyển cho shop '{shopName}'.  " +
                                $"Vui lòng thử lại sau hoặc liên hệ hỗ trợ.");
                        }

                        shippingFee = shippingResult.ShippingFee;
                        estimatedDeliveryText = shippingResult.ExpectedDeliveryTime;

                        _logger.LogInformation(
                            "✅ GHN Shipping calculated for Shop {ShopId}:  {Fee}đ, ETA: {ETA}",
                            shopId, shippingFee, estimatedDeliveryText);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "❌ Error calling GHN API for Shop {ShopId} ({ShopName}). " +
                            "From:  {FromDistrict}/{FromWard} → To: {ToDistrict}/{ToWard}",
                            shopId, shopName,
                            shopAddress.DistrictId, shopAddress.WardCode,
                            checkout.ToDistrictId, checkout.ToWardCode);

                        throw new InvalidOperationException(
                            $"Không thể tính phí vận chuyển cho shop '{shopName}'. " +
                            $"Chi tiết lỗi: {ex.Message}.  Vui lòng kiểm tra lại địa chỉ giao hàng hoặc liên hệ hỗ trợ.",
                            ex);
                    }

                    // ─────────────────────────────────────────────────
                    // 4.4: Tạo Order entity
                    // ─────────────────────────────────────────────────
                    var order = new Order
                    {
                        Id = Guid.NewGuid().ToString(),
                        OrderCode = await GenerateOrderCodeAsync(),
                        UserId = userId,
                        ShopId = shopId,

                        // Địa chỉ giao hàng
                        ShipToName = checkout.ShipToName,
                        ShipToPhone = checkout.ShipToPhone,
                        ShipToAddress = checkout.ShipToAddress,

                        // ⭐ GHN Address Format
                        ToProvinceId = checkout.ToProvinceId,
                        ToProvinceName = checkout.ToProvinceName,
                        ToDistrictId = checkout.ToDistrictId,
                        ToDistrictName = checkout.ToDistrictName,
                        ToWardCode = checkout.ToWardCode,
                        ToWardName = checkout.ToWardName,
                        ServiceTypeId = checkout.ServiceTypeId,

                        // Pricing
                        Subtotal = subtotal,
                        ShippingFee = shippingFee,  // ⭐ PHÍ SHIP ĐỘNG TỪ GHN
                        Discount = 0,
                        Total = subtotal + shippingFee,

                        // Status
                        Status = OrderStatus.Pending,
                        PaymentStatus = PaymentStatus.Pending,
                        BalanceReleased = false,
                        CreatedAt = DateTime.UtcNow,

                        // ⭐ Thông tin giao hàng
                        EstimatedDeliveryText = estimatedDeliveryText
                    };

                    await _uow.Orders.AddAsync(order);

                    _logger.LogInformation(
                        "📝 Order {OrderCode} created:  {Subtotal}đ + {Ship}đ = {Total}đ",
                        order.OrderCode, subtotal, shippingFee, order.Total);

                    // ─────────────────────────────────────────────────
                    // 4.5: Tạo OrderDetails và giảm stock
                    // ─────────────────────────────────────────────────
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

                        _logger.LogDebug(
                            "  - {ProductName} x{Qty} @ {Price}đ",
                            item.Product.Name, item.Quantity, item.Product.Price);
                    }

                    createdOrders.Add(order);
                }

                await _uow.CompleteAsync();

                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                // STEP 5: Áp dụng Voucher (nếu có)
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                VoucherApplyResultDTO? voucherResult = null;
                var voucherCode = (checkout.VoucherCode ?? string.Empty).Trim();

                if (!string.IsNullOrWhiteSpace(voucherCode))
                {
                    _logger.LogInformation("🎫 Applying voucher: {Code}", voucherCode);

                    voucherResult = await _voucherService.ValidateAndPreviewAsync(userId, voucherCode, createdOrders);

                    if (!voucherResult.IsValid)
                    {
                        _logger.LogWarning("⚠️ Voucher invalid: {Code} - {Error}",
                            voucherCode, voucherResult.ErrorMessage);

                        throw new InvalidOperationException(
                            voucherResult.ErrorMessage ?? "Voucher không hợp lệ.");
                    }

                    // Gán Discount cho từng order
                    foreach (var od in createdOrders)
                    {
                        var discount = voucherResult.OrderDiscounts
                            .FirstOrDefault(x => x.OrderId == od.Id)?.DiscountAmount ?? 0m;

                        od.Discount = discount;
                        od.Total = od.Subtotal + od.ShippingFee - od.Discount;
                        od.VoucherCodeUsed = voucherCode;

                        await _uow.Orders.UpdateAsync(od);

                        _logger.LogInformation(
                            "  ✅ Order {OrderCode}:  Discount={Discount}đ, NewTotal={Total}đ",
                            od.OrderCode, discount, od.Total);
                    }
                }

                await _uow.CompleteAsync();

                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                // STEP 6: Tính tổng
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                decimal totalShipping = createdOrders.Sum(o => o.ShippingFee);
                decimal grandTotal = createdOrders.Sum(o => o.Total);
                decimal totalDiscount = createdOrders.Sum(o => o.Discount);

                _logger.LogInformation(
                    "💰 Grand Total: {Total}đ (Shipping: {Ship}đ, Discount: {Discount}đ)",
                    grandTotal, totalShipping, totalDiscount);

                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                // STEP 7: Xử lý Payment
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                string method = (checkout.PaymentMethod ?? "PAYOS").ToUpper();
                string? paymentUrl = null;
                decimal walletUsed = 0;
                decimal payOSRequired = grandTotal;

                if (method == "WALLET")
                {
                    // ==================== WALLET ====================
                    bool ok = await _customerWalletService.HasSufficientBalanceAsync(userId, grandTotal);
                    if (!ok)
                        throw new InvalidOperationException("Số dư ví không đủ.");

                    await _customerWalletService.DeductBalanceAsync(
                        userId, grandTotal, WalletTransactionType.Payment,
                        string.Join(",", createdOrders.Select(o => o.Id)),
                        $"Thanh toán {createdOrders.Count} đơn hàng");

                    foreach (var o in createdOrders)
                    {
                        o.PaymentStatus = PaymentStatus.Paid;
                        o.Status = OrderStatus.Paid;
                        await _uow.Orders.UpdateAsync(o);
                    }

                    // Tạo transaction
                    var txObj = await CreateTransactionAsync(
                        createdOrders,
                        grandTotal,
                        "WALLET",
                        string.IsNullOrWhiteSpace(voucherCode) ? null : voucherCode);

                    txObj.Status = TransactionStatus.Completed;
                    txObj.CompletedAt = DateTime.UtcNow;
                    await _uow.Transactions.UpdateAsync(txObj);
                    await _uow.CompleteAsync();

                    // Distribute revenue
                    await DistributeRevenueToShopsAsync(createdOrders, txObj);

                    // Platform nhận hoa hồng
                    await _platformWalletService.AddCommissionAsync(
                        txObj.PlatformFeeAmount,
                        txObj.Id,
                        string.Join(",", createdOrders.Select(o => o.OrderCode))
                    );

                    // Mark voucher used
                    if (voucherResult?.IsValid == true && !string.IsNullOrWhiteSpace(voucherCode))
                    {
                        await _voucherService.MarkVoucherUsedAsync(
                            userId,
                            voucherCode,
                            createdOrders,
                            $"WALLET-{txObj.Id}");
                    }

                    walletUsed = grandTotal;
                    payOSRequired = 0;

                    // ⭐ Gamification Event
                    await _gamification.HandleEventAsync(userId, new GamificationEventDTO
                    {
                        Action = "PurchaseProduct",
                        ReferenceId = string.Join(",", createdOrders.Select(o => o.Id))
                    });

                    _logger.LogInformation("✅ Paid by WALLET: {Amount}đ", grandTotal);
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

                    _logger.LogInformation("💳 PayOS payment URL created");
                }

                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                // STEP 8: Xóa cart items
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                foreach (var item in selectedItems)
                    await _uow.CartItems.DeleteAsync(item);

                // Load lại User để có FullName
                var user = await _uow.Users.GetAsync(u => u.Id == userId);
                foreach (var o in createdOrders)
                    o.User = user;

                await _uow.CompleteAsync();
                await tx.CommitAsync();

                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                // STEP 9: Gửi thông báo
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                foreach (var order in createdOrders)
                {
                    var shop = await _uow.Shops.GetAsync(s => s.Id == order.ShopId);
                    if (shop != null && !string.IsNullOrEmpty(shop.SellerId))
                    {
                        await _notification.CreateAsync(
                            shop.SellerId,
                            "OrderNew",
                            $"Bạn có đơn hàng mới #{order.OrderCode}",
                            $"Khách hàng {order.User?.FullName ?? order.UserId} đã đặt đơn tổng {order.Total:N0}đ (ship: {order.ShippingFee:N0}đ)."
                        );
                    }
                }

                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                // STEP 10: Achievements
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                await _achievement.IncreaseProgressAsync(userId, "ACHV_FIRST_PURCHASE", 1);
                await _achievement.IncreaseProgressAsync(userId, "ACHV_5_PURCHASES", 1);
                await _achievement.IncreaseProgressAsync(userId, "ACHV_10_PURCHASES", 1);

                _logger.LogInformation(
                    "🎉 Checkout completed:  {OrderCount} orders, Total: {Total}đ",
                    createdOrders.Count, grandTotal);

                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                // STEP 11: Return result
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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
                    VoucherCodeUsed = voucherResult?.IsValid == true ? voucherCode : null
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "❌ Checkout failed for user {UserId}", userId);
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
                includeProperties: "Shop,User");

            if (order == null)
                throw new InvalidOperationException("Order không tìm thấy");

            // ✔ Parse status đúng
            var newStatus = Enum.Parse<OrderStatus>(status);

            order.Status = newStatus;
            await _uow.Orders.UpdateAsync(order);
            await _uow.CompleteAsync();

            // =======================================
            // 🔔 NOTIFICATION — Order Status to Buyer
            // =======================================
            string title = "";
            string? content = null;

            switch (newStatus)
            {
                case OrderStatus.Processing:
                    title = $"Đơn hàng #{order.OrderCode} đã được xác nhận";
                    content = $"Shop {order.Shop?.Name} đang chuẩn bị đơn.";
                    break;

                case OrderStatus.Shipping:
                    title = $"Đơn hàng #{order.OrderCode} đang được giao";
                    content = $"Đơn hàng đang trên đường đến bạn.";
                    break;

                case OrderStatus.Completed:
                    title = $"Đơn hàng #{order.OrderCode} đã giao thành công";
                    content = $"Cảm ơn bạn đã mua hàng tại {order.Shop?.Name}.";
                    break;
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                await _notification.CreateAsync(
                    order.UserId,
                    "OrderStatus",
                    title,
                    content
                );
            }

            return MapToDTO(order);
        }

        public async Task<OrderDTO> ConfirmReceivedAsync(string orderId, string userId)
        {
            var order = await _uow.Orders.GetAsync(
                o => o.Id == orderId,
                includeProperties: "User,Shop")
                ?? throw new InvalidOperationException("Order không tìm thấy.");

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

        public async Task<OrderDTO> CancelOrderAsync(string orderId, string userId, string cancelReason)
        {
            var order = await _uow.Orders.GetAsync(
                o => o.Id == orderId,
                includeProperties: "User,Shop,Details,Details.Product,Details.Product.Images,Details.Product.Category")
                ?? throw new InvalidOperationException("Order không tìm thấy.");

            // Kiểm tra quyền hủy đơn
            var user = await _uow.Users.GetAsync(u => u.Id == userId);
            bool isCustomer = order.UserId == userId;
            bool isSeller = user?.Shop?.Id == order.ShopId;

            if (!isCustomer && !isSeller)
                throw new UnauthorizedAccessException("Bạn không có quyền hủy đơn hàng này.");

            // Chỉ cho phép hủy đơn ở trạng thái Pending, Paid, Processing
            if (order.Status != OrderStatus.Pending &&
                order.Status != OrderStatus.Paid &&
                order.Status != OrderStatus.Processing)
                throw new InvalidOperationException(
                    "Không thể hủy đơn hàng ở trạng thái hiện tại.  Chỉ có thể hủy đơn Pending/Paid/Processing.");

            // Hoàn lại stock sản phẩm
            foreach (var detail in order.Details)
            {
                var product = detail.Product;
                if (product != null)
                {
                    product.Stock += detail.Quantity;
                    await _uow.Products.UpdateAsync(product);
                }
            }

            // Xử lý hoàn tiền nếu đã thanh toán
            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                // Hoàn tiền về CustomerWallet
                await _customerWalletService.AddBalanceAsync(
                    order.UserId,
                    order.Total,
                    order.Id,
                    $"Hoàn tiền đơn hàng {order.OrderCode} - Lý do: {cancelReason}");

                // Trừ tiền trong ShopWallet. PendingBalance
                await _shopWalletService.DeductPendingOnlyAsync(
                    order.ShopId,
                    order.Total - order.ShippingFee,
                    WalletTransactionType.Refund,
                    order.Id,
                    $"Hoàn tiền đơn hàng {order.OrderCode} - Lý do: {cancelReason}");

                order.PaymentStatus = PaymentStatus.Refunded;
            }

            // Cập nhật trạng thái đơn hàng
            order.Status = OrderStatus.Cancelled;
            await _uow.Orders.UpdateAsync(order);

            // Cập nhật transaction
            var tx = await _uow.Transactions.GetByOrderIdAsync(orderId);
            if (tx != null)
            {
                tx.Status = TransactionStatus.Cancelled;
                await _uow.Transactions.UpdateAsync(tx);
            }

            await _uow.CompleteAsync();

            // Gửi thông báo
            string notificationMessage = isCustomer
                ? $"Khách hàng đã hủy đơn hàng {order.OrderCode}.  Lý do: {cancelReason}"
                : $"Shop đã hủy đơn hàng {order.OrderCode}. Lý do: {cancelReason}";

            if (isCustomer && !string.IsNullOrEmpty(order.Shop?.SellerId))
            {
                await _notification.CreateAsync(
                    order.Shop.SellerId,
                    "Đơn hàng bị hủy",
                    notificationMessage,
                    $"/shop/orders/{order.Id}");
            }
            else if (isSeller)
            {
                await _notification.CreateAsync(
                    order.UserId,
                    "Đơn hàng bị hủy",
                    notificationMessage,
                    $"/orders/{order.Id}");
            }

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

                // ⭐ THÊM:  GHN address info
                ToProvinceId = o.ToProvinceId,
                ToProvinceName = o.ToProvinceName,
                ToDistrictId = o.ToDistrictId,
                ToDistrictName = o.ToDistrictName,
                ToWardCode = o.ToWardCode,
                ToWardName = o.ToWardName,

                Subtotal = o.Subtotal,
                ShippingFee = o.ShippingFee,
                Discount = o.Discount,
                Total = o.Total,
                Status = o.Status.ToString(),
                PaymentStatus = o.PaymentStatus.ToString(),
                BalanceReleased = o.BalanceReleased,
                CreatedAt = o.CreatedAt,
                CompletedAt = o.CompletedAt,

                // ⭐ THÊM: Shipping info
                EstimatedDeliveryText = o.EstimatedDeliveryText,
                EstimatedDeliveryDate = o.EstimatedDeliveryDate,
                ShippingTrackingCode = o.ShippingTrackingCode,
                ShippingStatus = o.ShippingStatus,

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

            decimal platformFeePercent = config.DefaultCommissionRate;
            decimal totalPlatformFee = totalAmount * platformFeePercent / 100;
            decimal totalShopAmount = totalAmount - totalPlatformFee;

            // ==========================
            // 1) Tạo TRANSACTION (master)
            // ==========================
            var tx = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                TotalAmount = totalAmount,
                PlatformFeePercent = platformFeePercent,
                PlatformFeeAmount = totalPlatformFee,
                ShopAmount = totalShopAmount,
                Status = TransactionStatus.Pending,
                PaymentMethod = method,
                VoucherCode = voucherCode,
                CreatedAt = DateTime.UtcNow,
            };

            await _uow.Transactions.AddAsync(tx);
            await _uow.CompleteAsync();

            // ==========================
            // 2) Mapping nhiều Order vào TransactionOrder
            // ==========================
            foreach (var order in orders)
            {
                var mapping = new TransactionOrder
                {
                    Id = Guid.NewGuid().ToString(),
                    TransactionId = tx.Id,
                    OrderId = order.Id
                };

                await _uow.TransactionOrders.AddAsync(mapping);

                // ================================
                // 3) Breakdown theo từng Order
                // ================================
                decimal orderTotal = order.Total;
                decimal orderFee = orderTotal * platformFeePercent / 100;
                decimal orderShopAmount = orderTotal - orderFee;

                var breakdown = new TransactionOrderBreakdown
                {
                    Id = Guid.NewGuid().ToString(),
                    TransactionOrderId = mapping.Id,
                    TotalAmount = orderTotal,
                    PlatformFeeAmount = orderFee,
                    ShopAmount = orderShopAmount,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.TransactionOrderBreakdowns.AddAsync(breakdown);
            }

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
