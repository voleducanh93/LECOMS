using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    [Index(nameof(Code), IsUnique = true)]
    public class Voucher
    {
        [Key] public string Id { get; set; }

        [Required, MaxLength(50)] public string Code { get; set; } = null!;
        [Precision(18, 2)] public decimal? DiscountAmount { get; set; }
        public int? DiscountPercent { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUsagePerUser { get; set; }

        public byte Active { get; set; } = 1;
        public ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();
    }
}
