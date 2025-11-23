using System;
using System.Collections.Generic;

namespace LECOMS.Data.DTOs.Transaction
{
    public class TransactionDTO
    {
        public string Id { get; set; } = null!;

        /// <summary>
        /// Danh sách OrderId mà Transaction này liên quan
        /// </summary>
        public List<string> OrderIds { get; set; } = new();

        /// <summary>
        /// Danh sách OrderCode để hiển thị dễ hơn (OPTIONAL — nhưng rất hữu ích)
        /// </summary>
        public List<string>? OrderCodes { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal PlatformFeePercent { get; set; }
        public decimal PlatformFeeAmount { get; set; }
        public decimal ShopAmount { get; set; }

        public string Status { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;

        public string? PayOSTransactionId { get; set; }
        public long? PayOSOrderCode { get; set; }

        public string? PayOSPaymentUrl { get; set; }
        public string? VoucherCode { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
