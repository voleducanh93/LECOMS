using System;

namespace LECOMS.Data.Entities
{
    public class TransactionOrderBreakdown
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // ⭐ LINK TO TransactionOrder — đúng chuẩn marketplace
        public string TransactionOrderId { get; set; } = null!;
        public TransactionOrder TransactionOrder { get; set; } = null!;

        // ⭐ Breakdown theo từng Order
        public decimal TotalAmount { get; set; }
        public decimal PlatformFeeAmount { get; set; }
        public decimal ShopAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
