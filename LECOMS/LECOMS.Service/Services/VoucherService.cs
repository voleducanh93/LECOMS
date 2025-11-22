using LECOMS.Data.DTOs.Voucher;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<VoucherService> _logger;

        public VoucherService(IUnitOfWork uow, ILogger<VoucherService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        // =========================================================
        // 1. VALIDATE + PREVIEW DISCOUNT
        // =========================================================
        public async Task<VoucherApplyResultDTO> ValidateAndPreviewAsync(
            string userId,
            string voucherCode,
            IEnumerable<Order> orders)
        {
            var result = new VoucherApplyResultDTO
            {
                VoucherCode = voucherCode
            };

            if (string.IsNullOrWhiteSpace(voucherCode))
            {
                result.IsValid = false;
                result.ErrorCode = "NO_CODE";
                result.ErrorMessage = "Voucher code is required.";
                return result;
            }

            var voucher = await _uow.Vouchers.GetByCodeAsync(voucherCode.Trim());
            if (voucher == null)
            {
                result.IsValid = false;
                result.ErrorCode = "NOT_FOUND";
                result.ErrorMessage = "Voucher không tồn tại.";
                return result;
            }

            var now = DateTime.UtcNow;

            if (!voucher.IsActive ||
                voucher.StartDate > now ||
                (voucher.EndDate.HasValue && voucher.EndDate.Value < now))
            {
                result.IsValid = false;
                result.ErrorCode = "EXPIRED";
                result.ErrorMessage = "Voucher đã hết hạn hoặc không còn hiệu lực.";
                return result;
            }

            if (voucher.QuantityAvailable <= 0)
            {
                result.IsValid = false;
                result.ErrorCode = "OUT_OF_STOCK";
                result.ErrorMessage = "Voucher đã hết lượt sử dụng.";
                return result;
            }

            // Nếu có giới hạn per-user → kiểm tra UserVoucher
            if (voucher.UsageLimitPerUser.HasValue && voucher.UsageLimitPerUser.Value > 0)
            {
                var usedCount = await _uow.UserVouchers.CountAsync(uv =>
                    uv.UserId == userId &&
                    uv.VoucherId == voucher.Id &&
                    uv.IsUsed);

                if (usedCount >= voucher.UsageLimitPerUser.Value)
                {
                    result.IsValid = false;
                    result.ErrorCode = "USER_LIMIT";
                    result.ErrorMessage = "Bạn đã đạt giới hạn sử dụng voucher này.";
                    return result;
                }
            }

            var orderList = orders.ToList();
            if (!orderList.Any())
            {
                result.IsValid = false;
                result.ErrorCode = "NO_ORDER";
                result.ErrorMessage = "Không có đơn hàng nào để áp dụng voucher.";
                return result;
            }

            // 👉 Nếu có khóa học trong order và bạn muốn cấm, có thể filter ở đây
            // if (orderList.Any(o => o.Details.Any(d => d.Product.IsCourse))) { ... }

            // Theo business: voucher áp dụng trên tổng Subtotal (không gồm shipping)
            decimal productTotal = orderList.Sum(o => o.Subtotal);

            if (productTotal <= 0)
            {
                result.IsValid = false;
                result.ErrorCode = "EMPTY";
                result.ErrorMessage = "Giá trị đơn hàng không hợp lệ.";
                return result;
            }

            if (voucher.MinOrderAmount.HasValue &&
                productTotal < voucher.MinOrderAmount.Value)
            {
                result.IsValid = false;
                result.ErrorCode = "MIN_ORDER";
                result.ErrorMessage =
                    $"Đơn hàng phải đạt tối thiểu {voucher.MinOrderAmount.Value:N0} để dùng voucher.";
                return result;
            }

            // ================= TÍNH DISCOUNT =================
            decimal rawDiscount = 0m;

            switch (voucher.DiscountType)
            {
                case DiscountType.Percentage:
                    rawDiscount = productTotal * voucher.DiscountValue / 100m;
                    break;

                case DiscountType.FixedAmount:
                    rawDiscount = voucher.DiscountValue;
                    break;
            }

            if (voucher.MaxDiscountAmount.HasValue &&
                rawDiscount > voucher.MaxDiscountAmount.Value)
            {
                rawDiscount = voucher.MaxDiscountAmount.Value;
            }

            if (rawDiscount <= 0)
            {
                result.IsValid = false;
                result.ErrorCode = "ZERO";
                result.ErrorMessage = "Voucher không tạo ra giảm giá.";
                return result;
            }

            // Phân bổ discount theo tỉ lệ Subtotal từng order
            decimal remaining = rawDiscount;
            for (int i = 0; i < orderList.Count; i++)
            {
                var order = orderList[i];
                decimal ratio = order.Subtotal / productTotal;

                decimal discountForOrder = (i == orderList.Count - 1)
                    ? remaining   // đơn cuối ăn phần còn lại
                    : Math.Round(rawDiscount * ratio, 0);

                remaining -= discountForOrder;

                // Không cho giảm > (Subtotal + ShippingFee)
                var maxDiscountForOrder = order.Subtotal + order.ShippingFee;
                if (discountForOrder > maxDiscountForOrder)
                    discountForOrder = maxDiscountForOrder;

                result.OrderDiscounts.Add(new OrderDiscountDTO
                {
                    OrderId = order.Id,
                    DiscountAmount = discountForOrder
                });
            }

            result.IsValid = true;
            result.TotalDiscount = rawDiscount;
            return result;
        }

        // =========================================================
        // 2. ĐÁNH DẤU VOUCHER ĐÃ DÙNG
        // =========================================================
        public async Task MarkVoucherUsedAsync(
            string userId,
            string voucherCode,
            IEnumerable<Order> orders,
            string paymentReference)
        {
            if (string.IsNullOrWhiteSpace(voucherCode))
                return;

            var voucher = await _uow.Vouchers.GetByCodeAsync(voucherCode.Trim());
            if (voucher == null)
            {
                _logger.LogWarning("MarkVoucherUsedAsync: voucher {Code} not found", voucherCode);
                return;
            }

            // Trừ quantity global
            if (voucher.QuantityAvailable > 0)
            {
                voucher.QuantityAvailable -= 1;
                await _uow.Vouchers.UpdateAsync(voucher);
            }

            // Nếu user đã có UserVoucher chưa dùng → đánh dấu dùng
            var userVoucher = await _uow.UserVouchers.GetAsync(uv =>
                uv.UserId == userId &&
                uv.VoucherId == voucher.Id &&
                !uv.IsUsed);

            if (userVoucher != null)
            {
                userVoucher.IsUsed = true;
                userVoucher.UsedAt = DateTime.UtcNow;
                userVoucher.OrderId = string.Join(",", orders.Select(o => o.Id));
                await _uow.UserVouchers.UpdateAsync(userVoucher);
            }
            else
            {
                // Public voucher
                var newUv = new UserVoucher
                {
                    UserId = userId,
                    VoucherId = voucher.Id,
                    AssignedAt = DateTime.UtcNow,
                    IsUsed = true,
                    UsedAt = DateTime.UtcNow,
                    OrderId = string.Join(",", orders.Select(o => o.Id))
                };
                await _uow.UserVouchers.AddAsync(newUv);
            }

            await _uow.CompleteAsync();

            _logger.LogInformation(
                "Voucher {Code} used by user {UserId} for orders {Orders}, paymentRef={Ref}",
                voucherCode, userId, string.Join(",", orders.Select(o => o.OrderCode)), paymentReference);
        }

        // =========================================================
        // 3. LẤY DANH SÁCH VOUCHER CỦA USER
        // =========================================================
        public async Task<IEnumerable<UserVoucherDTO>> GetMyVouchersAsync(string userId)
        {
            var list = await _uow.UserVouchers.GetAllAsync(
                uv => uv.UserId == userId,
                includeProperties: "Voucher");

            var now = DateTime.UtcNow;

            return list.Select(uv =>
            {
                var v = uv.Voucher;
                bool expired = v.EndDate.HasValue && v.EndDate.Value < now;

                return new UserVoucherDTO
                {
                    Code = v.Code,
                    DiscountType = v.DiscountType.ToString(),
                    DiscountValue = v.DiscountValue,
                    MinOrderAmount = v.MinOrderAmount,
                    MaxDiscountAmount = v.MaxDiscountAmount,
                    StartDate = v.StartDate,
                    EndDate = v.EndDate,
                    IsUsed = uv.IsUsed,
                    IsExpired = expired,
                    AssignedAt = uv.AssignedAt,
                    UsedAt = uv.UsedAt
                };
            }).ToList();
        }
    }
}
