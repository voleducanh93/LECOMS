using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Ví của Shop
    /// </summary>
    [Index(nameof(ShopId), IsUnique = true)]
    public class ShopWallet
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public int ShopId { get; set; }

        [ForeignKey(nameof(ShopId))]
        public Shop Shop { get; set; } = null!;

        [Precision(18, 2)]
        public decimal AvailableBalance { get; set; } = 0;

        [Precision(18, 2)]
        public decimal PendingBalance { get; set; } = 0;

        [Precision(18, 2)]
        public decimal TotalEarned { get; set; } = 0;

        [Precision(18, 2)]
        public decimal TotalWithdrawn { get; set; } = 0;

        [Precision(18, 2)]
        public decimal TotalRefunded { get; set; } = 0;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
        public ICollection<WithdrawalRequest> WithdrawalRequests { get; set; } = new List<WithdrawalRequest>();
    }
}