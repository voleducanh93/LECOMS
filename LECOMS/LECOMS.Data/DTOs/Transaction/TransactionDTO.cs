using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Transaction
{
    public class TransactionDTO
    {
        public string Id { get; set; } = null!;
        public string OrderId { get; set; } = null!;
        public string? OrderCode { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PlatformFeePercent { get; set; }
        public decimal PlatformFeeAmount { get; set; }
        public decimal ShopAmount { get; set; }
        public string Status { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string? PayOSTransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
