using LECOMS.Data.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LECOMS.Data.Entities
{
    [Index(nameof(Code), IsUnique = true)]
    public class Voucher
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required, MaxLength(50)]
        public string Code { get; set; } = null!;

        //========================
        //  QUY ĐỊNH GIẢM GIÁ
        //========================
        public DiscountType DiscountType { get; set; }   // Percentage / FixedAmount

        /// <summary>
        /// Nếu %: DiscountValue = số phần trăm
        /// Nếu cố định: DiscountValue = số tiền VND
        /// </summary>
        [Precision(18, 2)]
        public decimal DiscountValue { get; set; }

        /// <summary>Giảm tối đa (áp dụng cho %)</summary>
        [Precision(18, 2)]
        public decimal? MaxDiscountAmount { get; set; }

        /// <summary>Đơn hàng tối thiểu</summary>
        [Precision(18, 2)]
        public decimal? MinOrderAmount { get; set; }

        /// <summary>Mỗi user được dùng tối đa bao nhiêu lần</summary>
        public int? UsageLimitPerUser { get; set; }

        /// <summary>Tổng lượt voucher có thể dùng</summary>
        public int QuantityAvailable { get; set; }

        /// <summary>Thời gian bắt đầu – kết thúc</summary>
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        /// <summary>Voucher còn hoạt động?</summary>
        public bool IsActive { get; set; } = true;

        //========================
        // NAVIGATION
        //========================
        public ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();
    }
}
