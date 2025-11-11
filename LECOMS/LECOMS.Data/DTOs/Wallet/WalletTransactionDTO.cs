using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Wallet
{
    public class WalletTransactionDTO
    {
        public string Id { get; set; } = null!;
        public string Type { get; set; } = null!;
        public decimal Amount { get; set; }
        public string? BalanceType { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Description { get; set; } = null!;
        public string? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
