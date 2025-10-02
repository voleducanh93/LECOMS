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
    [Index(nameof(PointWalletId), nameof(CreatedAt))]
    public class PointLedger
    {
        [Key] public string Id { get; set; }

        [Required] public string PointWalletId { get; set; }
        [ForeignKey(nameof(PointWalletId))] public PointWallet Wallet { get; set; } = null!;

        public PointLedgerType Type { get; set; }
        public int Points { get; set; }
        public int BalanceAfter { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
