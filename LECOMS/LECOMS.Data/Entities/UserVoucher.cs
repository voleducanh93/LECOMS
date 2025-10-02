using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    [Index(nameof(UserId), nameof(VoucherId), IsUnique = true)]
    public class UserVoucher
    {
        [Key] public string Id { get; set; }

        [Required] public string UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        [Required] public string VoucherId { get; set; }
        [ForeignKey(nameof(VoucherId))] public Voucher Voucher { get; set; } = null!;

        public bool IsUsed { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
