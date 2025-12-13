using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Địa chỉ kho/cửa hàng của Shop (để tính phí ship)
    /// </summary>
    public class ShopAddress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ShopId { get; set; }

        [ForeignKey(nameof(ShopId))]
        public Shop Shop { get; set; } = null!;

        // ============ GHN ADDRESS FORMAT ============

        /// <summary>
        /// Mã tỉnh/thành phố (GHN ProvinceID)
        /// </summary>
        public int ProvinceId { get; set; }

        [Required, MaxLength(200)]
        public string ProvinceName { get; set; } = null!;

        /// <summary>
        /// Mã quận/huyện (GHN DistrictID) - BẮT BUỘC
        /// </summary>
        public int DistrictId { get; set; }

        [Required, MaxLength(200)]
        public string DistrictName { get; set; } = null!;

        /// <summary>
        /// Mã phường/xã (GHN WardCode) - BẮT BUỘC
        /// </summary>
        [Required, MaxLength(50)]
        public string WardCode { get; set; } = null!;

        [Required, MaxLength(200)]
        public string WardName { get; set; } = null!;

        /// <summary>
        /// Địa chỉ chi tiết (số nhà, tên đường)
        /// </summary>
        [Required, MaxLength(500)]
        public string DetailAddress { get; set; } = null!;

        /// <summary>
        /// Có phải địa chỉ mặc định không (dùng cho tính ship)
        /// </summary>
        public bool IsDefault { get; set; } = true;

        /// <summary>
        /// Tên người liên hệ tại kho
        /// </summary>
        [MaxLength(200)]
        public string? ContactName { get; set; }

        /// <summary>
        /// Số điện thoại liên hệ
        /// </summary>
        [MaxLength(20)]
        public string? ContactPhone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
