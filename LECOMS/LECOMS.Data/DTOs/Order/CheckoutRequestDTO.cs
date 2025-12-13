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

        // ⭐⭐⭐ THÊM MỚI: GHN Address
        /// <summary>
        /// Mã tỉnh/thành phố (GHN ProvinceID) - Optional
        /// </summary>
        public int? ToProvinceId { get; set; }

        /// <summary>
        /// Tên tỉnh/thành phố
        /// </summary>
        public string? ToProvinceName { get; set; }

        /// <summary>
        /// Mã quận/huyện (GHN DistrictID) - BẮT BUỘC
        /// </summary>
        public int ToDistrictId { get; set; }

        /// <summary>
        /// Tên quận/huyện
        /// </summary>
        public string? ToDistrictName { get; set; }

        /// <summary>
        /// Mã phường/xã (GHN WardCode) - BẮT BUỘC
        /// </summary>
        public string ToWardCode { get; set; } = null!;

        /// <summary>
        /// Tên phường/xã
        /// </summary>
        public string? ToWardName { get; set; }

        /// <summary>
        /// Loại dịch vụ vận chuyển:  2 = Express, 5 = Standard
        /// </summary>
        public int ServiceTypeId { get; set; } = 2;

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
