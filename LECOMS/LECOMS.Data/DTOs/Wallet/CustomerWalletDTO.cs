using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Wallet
{
    public class CustomerWalletDTO
    {
        public string Id { get; set; } = null!;
        public string CustomerId { get; set; } = null!;
        public string? CustomerName { get; set; }
        public decimal Balance { get; set; }
        public decimal TotalRefunded { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal TotalWithdrawn { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
