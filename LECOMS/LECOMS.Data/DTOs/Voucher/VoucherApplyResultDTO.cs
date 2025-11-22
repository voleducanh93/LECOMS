using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Voucher
{
    public class OrderDiscountDTO
    {
        public string OrderId { get; set; } = null!;
        public decimal DiscountAmount { get; set; }
    }

    public class VoucherApplyResultDTO
    {
        public string VoucherCode { get; set; } = null!;
        public bool IsValid { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal TotalDiscount { get; set; }
        public List<OrderDiscountDTO> OrderDiscounts { get; set; } = new();
    }

    public class UserVoucherDTO
    {
        public string Code { get; set; } = null!;
        public string DiscountType { get; set; } = null!;
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsUsed { get; set; }
        public bool IsExpired { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}
