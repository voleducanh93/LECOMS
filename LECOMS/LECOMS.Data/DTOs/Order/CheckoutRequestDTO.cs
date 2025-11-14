using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LECOMS.Data.DTOs.Order
{
    public class CheckoutRequestDTO
    {
        // ============ SHIPPING INFO ============
        [Required, MaxLength(255)]
        public string ShipToName { get; set; } = null!;

        [Required, MaxLength(20)]
        public string ShipToPhone { get; set; } = null!;

        [Required, MaxLength(500)]
        public string ShipToAddress { get; set; } = null!;

        // ============ VOUCHER (Optional) ============

        /// <summary>
        /// Mã voucher/giảm giá (nếu có)
        /// Hệ thống sẽ validate và tính discount tự động
        /// </summary>
        [MaxLength(50)]
        public string? VoucherCode { get; set; }

        // ============ PRODUCT SELECTION (Optional) ============

        /// <summary>
        /// Danh sách ProductId được chọn
        /// - NULL/Empty: Checkout toàn bộ cart
        /// - Có data: Chỉ checkout các sản phẩm này
        /// </summary>
        public List<string>? SelectedProductIds { get; set; }

        // ============ PAYMENT METHOD ============

        /// <summary>
        /// Phương thức thanh toán:
        /// - "PayOS": Thanh toán qua PayOS (mặc định)
        /// - "Wallet": Thanh toán bằng CustomerWallet
        /// - "Mixed": Wallet trước, thiếu thì PayOS
        /// </summary>
        [Required]
        public string PaymentMethod { get; set; } = "PayOS";

        /// <summary>
        /// Số tiền muốn dùng từ Wallet (khi PaymentMethod = "Mixed")
        /// Mặc định: Dùng toàn bộ balance
        /// </summary>
        public decimal? WalletAmountToUse { get; set; }

        // ============ NOTE (Optional) ============

        [MaxLength(500)]
        public string? Note { get; set; }
    }
}