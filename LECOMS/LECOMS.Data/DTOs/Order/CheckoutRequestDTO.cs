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

        [MaxLength(50)]
        public string? VoucherCode { get; set; }

        // ============ PRODUCT SELECTION (Optional) ============

        /// <summary>
        /// NULL / empty: checkout toàn bộ cart
        /// Có data: checkout các sản phẩm có trong list
        /// </summary>
        public List<string>? SelectedProductIds { get; set; }

        // ============ PAYMENT METHOD ============

        /// <summary>
        /// PAYOS / WALLET
        /// </summary>
        [Required]
        public string PaymentMethod { get; set; } = "PAYOS";

        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
