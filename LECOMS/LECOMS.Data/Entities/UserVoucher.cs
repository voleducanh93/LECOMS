using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LECOMS.Data.Entities
{
    [Index(nameof(UserId), nameof(VoucherId), IsUnique = true)]
    public class UserVoucher
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        //====================
        // RELATIONSHIPS
        //====================
        [Required]
        public string UserId { get; set; } = null!;
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Required]
        public string VoucherId { get; set; } = null!;
        [ForeignKey(nameof(VoucherId))]
        public Voucher Voucher { get; set; } = null!;

        //====================
        // USAGE STATUS
        //====================
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }

        /// <summary>Danh sách OrderId (csv) khi dùng voucher</summary>
        public string? OrderId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
