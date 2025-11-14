using System;
using System.Collections.Generic;

namespace LECOMS.Data.DTOs.Order
{
    public class CheckoutResultDTO
    {
        /// <summary>
        /// Danh sách orders được tạo
        /// </summary>
        public List<OrderDTO> Orders { get; set; } = new List<OrderDTO>();

        /// <summary>
        /// Payment URL (nếu dùng PayOS)
        /// NULL nếu thanh toán bằng Wallet
        /// </summary>
        public string? PaymentUrl { get; set; }

        /// <summary>
        /// Tổng tiền phải thanh toán
        /// </summary>
        public decimal TotalAmount { get; set; }

        // ⭐ THÊM MỚI: Payment breakdown

        /// <summary>
        /// Phương thức thanh toán đã dùng
        /// </summary>
        public string PaymentMethod { get; set; } = null!;

        /// <summary>
        /// Số tiền đã thanh toán từ Wallet
        /// </summary>
        public decimal WalletAmountUsed { get; set; }

        /// <summary>
        /// Số tiền cần thanh toán qua PayOS
        /// </summary>
        public decimal PayOSAmountRequired { get; set; }

        /// <summary>
        /// Discount đã áp dụng
        /// </summary>
        public decimal DiscountApplied { get; set; }

        /// <summary>
        /// Phí ship
        /// </summary>
        public decimal ShippingFee { get; set; }

        /// <summary>
        /// Voucher code đã dùng
        /// </summary>
        public string? VoucherCodeUsed { get; set; }
    }
}