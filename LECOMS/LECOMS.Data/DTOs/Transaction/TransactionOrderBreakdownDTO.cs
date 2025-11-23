using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Transaction
{
    public class TransactionOrderBreakdownDTO
    {
        public string TransactionId { get; set; }
        public string OrderId { get; set; }

        public decimal OrderAmount { get; set; }
        public decimal PlatformFeePercent { get; set; }
        public decimal PlatformFeeAmount { get; set; }
        public decimal ShopAmount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
