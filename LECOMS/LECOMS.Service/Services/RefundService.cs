using LECOMS.Data.DTOs.Refund;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class RefundService : IRefundService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<RefundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public RefundService(IUnitOfWork uow, ILogger<RefundService> logger, IServiceProvider serviceProvider)
        {
            _uow = uow;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        // ============================================================
        // 1. CUSTOMER TẠO YÊU CẦU REFUND
        // ============================================================
        public async Task<RefundRequestDTO> CreateAsync(string customerId, CreateRefundRequestDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OrderId))
                throw new ArgumentException("OrderId is required");

            var order = await _uow.Orders.GetAsync(
                o => o.Id == dto.OrderId,
                includeProperties: "Shop,User");

            if (order == null)
                throw new InvalidOperationException("Đơn đặt hàng không được tìm thấy.");

            if (order.UserId != customerId)
                throw new InvalidOperationException("Bạn không được phép hoàn trả đơn đặt hàng này.");

            if (order.PaymentStatus != PaymentStatus.Paid)
                throw new InvalidOperationException("Chỉ những đơn hàng đã thanh toán mới có thể được hoàn lại.");

            if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Refunded)
                throw new InvalidOperationException("Đơn đặt hàng không được hoàn lại.");

            // Check refund window
            var config = await _uow.PlatformConfigs.GetConfigAsync();
            if (config.MaxRefundDays > 0)
            {
                var baseTime = order.CompletedAt ?? order.CreatedAt;
                if (baseTime.AddDays(config.MaxRefundDays) < DateTime.UtcNow)
                    throw new InvalidOperationException("Thời hạn hoàn tiền đã hết hạn.");
            }

            // tránh duplicate pending refund
            var existing = await _uow.RefundRequests.GetAsync(r =>
                r.OrderId == order.Id &&
                (r.Status == RefundStatus.PendingShop ||
                 r.Status == RefundStatus.PendingAdmin ||
                 r.Status == RefundStatus.ShopApproved));

            if (existing != null)
                throw new InvalidOperationException("Hiện đã có yêu cầu hoàn tiền đang chờ xử lý.");

            decimal amount = dto.Type == RefundType.Full
                ? order.Total
                : Math.Min(dto.RefundAmount, order.Total);

            var refund = new RefundRequest
            {
                OrderId = order.Id,
                RequestedBy = customerId,
                ReasonType = dto.ReasonType,
                ReasonDescription = dto.ReasonDescription,
                Type = dto.Type,
                RefundAmount = amount,
                AttachmentUrls = dto.AttachmentUrls,
                Status = RefundStatus.PendingShop
            };

            await _uow.RefundRequests.AddAsync(refund);
            await _uow.CompleteAsync();

            refund = await _uow.RefundRequests.GetAsync(
                x => x.Id == refund.Id,
                includeProperties: "Order,RequestedByUser,ShopResponseByUser,AdminResponseByUser");

            return MapToDto(refund);
        }

        // ============================================================
        // 2. CUSTOMER VIEW REFUNDS
        // ============================================================
        public async Task<IEnumerable<RefundRequestDTO>> GetMyAsync(string customerId)
        {
            var list = await _uow.RefundRequests.GetAllAsync(
                r => r.RequestedBy == customerId,
                includeProperties: "Order,RequestedByUser");

            return list
                .OrderByDescending(x => x.RequestedAt)
                .Select(MapToDto)
                .ToList();
        }

        // ============================================================
        // 3. SHOP VIEW REFUNDS
        // ============================================================
        public async Task<IEnumerable<RefundRequestDTO>> GetForShopAsync(string sellerId)
        {
            var refunds = await _uow.RefundRequests.GetAllAsync(
                r => r.Order.Shop.SellerId == sellerId,
                includeProperties: "Order,RequestedByUser,ShopResponseByUser");

            return refunds
                .OrderByDescending(x => x.RequestedAt)
                .Select(MapToDto)
                .ToList();
        }

        // ============================================================
        // 4. SELLER DECISION (Approve / Reject)
        // ============================================================
        public async Task<RefundRequestDTO> SellerDecisionAsync(
            string refundId,
            string sellerId,
            bool approve,
            string? rejectReason)
        {
            var refund = await _uow.RefundRequests.GetAsync(
                r => r.Id == refundId,
                includeProperties: "Order,Order.Shop,RequestedByUser");

            if (refund == null)
                throw new InvalidOperationException("Không tìm thấy khoản tiền hoàn lại.");

            if (refund.Status != RefundStatus.PendingShop)
                throw new InvalidOperationException("Hoàn tiền không có trong Cửa hàng đang chờ xử lý.");

            if (refund.Order.Shop.SellerId != sellerId)
                throw new InvalidOperationException("Bạn không được phép xử lý khoản hoàn trả này.");

            refund.ShopResponseBy = sellerId;
            refund.ShopRespondedAt = DateTime.UtcNow;

            if (!approve)
            {
                refund.Status = RefundStatus.ShopRejected;
                refund.ShopRejectReason = rejectReason;
            }
            else
            {
                refund.Status = RefundStatus.PendingAdmin;
            }

            await _uow.RefundRequests.UpdateAsync(refund);
            await _uow.CompleteAsync();

            refund = await _uow.RefundRequests.GetAsync(
                x => x.Id == refundId,
                includeProperties: "Order,RequestedByUser,ShopResponseByUser");

            return MapToDto(refund);
        }

        // ============================================================
        // 5. ADMIN APPROVE / REJECT — Refund to CUSTOMER WALLET + TRỪ SHOP WALLET + HOÀN PHÍ SÀN
        // ============================================================
        public async Task<RefundRequestDTO> AdminDecisionAsync(
            string refundId,
            string adminId,
            bool approve,
            string? rejectReason)
        {
            var refund = await _uow.RefundRequests.GetAsync(
                r => r.Id == refundId,
                includeProperties: "Order,RequestedByUser,Order.Shop");

            if (refund == null)
                throw new InvalidOperationException("Không tìm thấy khoản tiền hoàn lại.");

            if (refund.Status != RefundStatus.PendingAdmin)
                throw new InvalidOperationException("Hoàn tiền không có trong Quản trị viên đang chờ xử lý.");

            refund.AdminResponseBy = adminId;
            refund.AdminRespondedAt = DateTime.UtcNow;

            if (!approve)
            {
                refund.Status = RefundStatus.AdminRejected;
                refund.AdminRejectReason = rejectReason;

                await _uow.RefundRequests.UpdateAsync(refund);
                await _uow.CompleteAsync();
                return MapToDto(refund);
            }

            var shopWalletService = _serviceProvider.GetRequiredService<IShopWalletService>();
            var customerWalletService = _serviceProvider.GetRequiredService<ICustomerWalletService>();
            var platformWalletService = _serviceProvider.GetRequiredService<IPlatformWalletService>();
            var config = await _uow.PlatformConfigs.GetConfigAsync();

            // ============================================
            // 1) Tính lại shopAmount + platformFee theo đơn hàng
            // ============================================
            decimal total = refund.Order.Total;
            decimal platformFee = total * config.DefaultCommissionRate / 100;
            decimal shopAmount = total - platformFee;

            decimal shopRefund = shopAmount;
            decimal platformRefund = platformFee;

            if (refund.Type == RefundType.Partial)
            {
                decimal ratio = refund.RefundAmount / total;
                shopRefund = Math.Round(shopAmount * ratio, 2);
                platformRefund = Math.Round(platformFee * ratio, 2);
            }

            // ============================================
            // 2) Trừ SHOP pending balance
            // ============================================
            await shopWalletService.DeductPendingOnlyAsync(
                refund.Order.ShopId,
                shopRefund,
                WalletTransactionType.Refund,
                refund.OrderId,
                $"Refund (Shop Portion) for Order {refund.Order.OrderCode}"
            );

            // ============================================
            // 3) HOÀN PHÍ SÀN → PLATFORM WALLET
            // ============================================
            await platformWalletService.RefundCommissionAsync(
                platformRefund,
                refund.Id,
                $"Hoàn tiền hoa hồng cho đơn hàng {refund.Order.OrderCode}"
            );

            // ============================================
            // 4) HOÀN TIỀN FULL CHO CUSTOMER
            // ============================================
            await customerWalletService.AddBalanceAsync(
                refund.RequestedBy,
                refund.RefundAmount,
                refund.Id,
                $"Hoàn tiền cho đơn hàng {refund.Order.OrderCode}"
            );

            // ============================================
            // 5) Cập nhật REFUND + ORDER
            // ============================================
            refund.Status = RefundStatus.Refunded;
            refund.ProcessNote = "Refund completed";

            refund.Order.Status = OrderStatus.Refunded;
            refund.Order.PaymentStatus = PaymentStatus.Refunded;

            await _uow.RefundRequests.UpdateAsync(refund);
            await _uow.Orders.UpdateAsync(refund.Order);
            await _uow.CompleteAsync();

            return MapToDto(refund);
        }

        // ============================================================
        // 6. CUSTOMER THÊM / CẬP NHẬT EVIDENCE (ẢNH/VIDEO)
        // ============================================================
        public async Task<RefundRequestDTO> AddEvidenceAsync(string refundId, string customerId, string[] urls)
        {
            var refund = await _uow.RefundRequests.GetAsync(
                r => r.Id == refundId,
                includeProperties: "Order,RequestedByUser,ShopResponseByUser,AdminResponseByUser");

            if (refund == null)
                throw new InvalidOperationException("Không tìm thấy khoản tiền hoàn lại.");

            if (refund.RequestedBy != customerId)
                throw new InvalidOperationException("Bạn không được phép sửa đổi khoản hoàn trả này.");

            // chỉ cho thêm evidence khi refund chưa kết thúc
            if (refund.Status == RefundStatus.ShopRejected ||
                refund.Status == RefundStatus.AdminRejected ||
                refund.Status == RefundStatus.Refunded)
            {
                throw new InvalidOperationException("Không thể thêm bằng chứng vào khoản hoàn trả đã đóng.");
            }

            if (urls == null || urls.Length == 0)
                return MapToDto(refund);

            var list = new List<string>();

            if (!string.IsNullOrWhiteSpace(refund.AttachmentUrls))
            {
                list.AddRange(
                    refund.AttachmentUrls
                        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            list.AddRange(urls.Where(u => !string.IsNullOrWhiteSpace(u)));

            // unique
            refund.AttachmentUrls = string.Join(";", list.Distinct());

            await _uow.RefundRequests.UpdateAsync(refund);
            await _uow.CompleteAsync();

            return MapToDto(refund);
        }

        // ============================================================
        // 7. ADMIN DASHBOARD STATS
        // ============================================================
        public async Task<RefundDashboardDTO> GetAdminDashboardAsync()
        {
            var list = await _uow.RefundRequests.GetAllAsync();

            var total = list.Count();
            var pendingShop = list.Count(r => r.Status == RefundStatus.PendingShop);
            var pendingAdmin = list.Count(r => r.Status == RefundStatus.PendingAdmin);
            var shopRejected = list.Count(r => r.Status == RefundStatus.ShopRejected);
            var adminRejected = list.Count(r => r.Status == RefundStatus.AdminRejected);
            var refunded = list.Count(r => r.Status == RefundStatus.Refunded);
            var totalAmount = list.Where(r => r.Status == RefundStatus.Refunded)
                                  .Sum(r => r.RefundAmount);

            var last30 = DateTime.UtcNow.AddDays(-30);
            var refundedLast30 = list.Where(r =>
                    r.Status == RefundStatus.Refunded &&
                    r.AdminRespondedAt.HasValue &&
                    r.AdminRespondedAt.Value >= last30);

            return new RefundDashboardDTO
            {
                TotalRequests = total,
                PendingShop = pendingShop,
                PendingAdmin = pendingAdmin,
                ShopRejected = shopRejected,
                AdminRejected = adminRejected,
                Refunded = refunded,
                TotalRefundAmount = totalAmount,
                RefundedLast30DaysCount = refundedLast30.Count(),
                RefundedLast30DaysAmount = refundedLast30.Sum(r => r.RefundAmount),
                GeneratedAtUtc = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<RefundRequestDTO>> GetPendingAdminAsync()
        {
            var list = await _uow.RefundRequests.GetAllAsync(
                r => r.Status == RefundStatus.PendingAdmin,
                includeProperties: "Order,RequestedByUser,ShopResponseByUser");

            return list.OrderBy(r => r.RequestedAt).Select(MapToDto).ToList();
        }

        public async Task<RefundRequestDTO?> GetByOrderIdAsync(string orderId, string userId)
        {
            var refund = await _uow.RefundRequests.GetAsync(
                r => r.OrderId == orderId,
                includeProperties: "Order,RequestedByUser,ShopResponseByUser,AdminResponseByUser");

            if (refund == null)
                return null;

            // Chỉ cho xem nếu là người mua hoặc là seller của shop đó
            bool isCustomer = refund.RequestedBy == userId;
            bool isSeller = refund.Order?.Shop?.SellerId == userId;

            if (!isCustomer && !isSeller)
                throw new InvalidOperationException("Bạn không có quyền xem refund của đơn này.");

            return MapToDto(refund);
        }


        // ============================================================
        // MAPPER
        // ============================================================
        private static RefundRequestDTO MapToDto(RefundRequest r)
        {
            return new RefundRequestDTO
            {
                Id = r.Id,
                OrderId = r.OrderId,
                OrderCode = r.Order?.OrderCode,

                RequestedBy = r.RequestedBy,
                RequestedByName = r.RequestedByUser?.FullName,
                RequestedAt = r.RequestedAt,

                ReasonType = r.ReasonType,
                ReasonDescription = r.ReasonDescription,
                Type = r.Type,
                RefundAmount = r.RefundAmount,
                AttachmentUrls = r.AttachmentUrls,

                Status = r.Status,

                ShopResponseBy = r.ShopResponseBy,
                ShopResponseByName = r.ShopResponseByUser?.FullName,
                ShopRespondedAt = r.ShopRespondedAt,
                ShopRejectReason = r.ShopRejectReason,

                ProcessedBy = r.AdminResponseBy,
                ProcessedByName = r.AdminResponseByUser?.FullName,
                ProcessedAt = r.AdminRespondedAt,
                ProcessNote = r.ProcessNote
            };
        }
    }
}
