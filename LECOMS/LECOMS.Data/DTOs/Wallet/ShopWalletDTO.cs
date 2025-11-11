using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Wallet
{
    public class ShopWalletDTO
    {
        public string Id { get; set; } = null!;
        public int ShopId { get; set; }
        public string? ShopName { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal TotalEarned { get; set; }
        public decimal TotalWithdrawn { get; set; }
        public decimal TotalRefunded { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
