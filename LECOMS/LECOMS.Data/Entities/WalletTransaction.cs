using LECOMS.Data.Enum;
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
    [Index(nameof(WalletAccountId), nameof(CreatedAt))]
    public class WalletTransaction
    {
        [Key] public string Id { get; set; }

        [Required] public string WalletAccountId { get; set; }
        [ForeignKey(nameof(WalletAccountId))] public WalletAccount Wallet { get; set; } = null!;

        [Required] public WalletTransactionType Type { get; set; }
        [Precision(18, 2)] public decimal Amount { get; set; }
        [Precision(18, 2)] public decimal BalanceAfter { get; set; }

        public string? OrderId { get; set; }   // optional link
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)] public string? Note { get; set; }
    }
}
