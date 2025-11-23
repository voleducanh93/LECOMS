using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Wallet
{
    public class PlatformWalletDTO
    {
        public decimal Balance { get; set; }
        public decimal TotalCommissionEarned { get; set; }
        public decimal TotalCommissionRefunded { get; set; }
        public decimal TotalPayout { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class PlatformWalletTransactionDTO
    {
        public string Id { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string ReferenceId { get; set; }
        public string ReferenceType { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PlatformWalletHistoryResponseDTO
    {
        public PlatformWalletDTO Wallet { get; set; }
        public IEnumerable<PlatformWalletTransactionDTO> Transactions { get; set; }
    }
}
