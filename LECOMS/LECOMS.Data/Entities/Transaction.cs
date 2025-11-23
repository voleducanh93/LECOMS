using LECOMS.Data.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LECOMS.Data.Entities
{
    [Index(nameof(PayOSTransactionId), IsUnique = true)]
    [Index(nameof(PayOSOrderCode), IsUnique = true)]
    [Index(nameof(Status))]
    [Index(nameof(CreatedAt))]
    public class Transaction
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // ============ RELATION MAPPING ============

        /// <summary>
        /// 1 Transaction có thể map tới N Orders qua bảng TransactionOrders
        /// </summary>
        public ICollection<TransactionOrder> TransactionOrders { get; set; }
            = new List<TransactionOrder>();

        public ICollection<TransactionOrderBreakdown> Breakdowns { get; set; }
            = new List<TransactionOrderBreakdown>();

        // ============ PAYMENT AMOUNTS ============

        [Precision(18, 2)]
        public decimal TotalAmount { get; set; }

        [Precision(5, 2)]
        public decimal PlatformFeePercent { get; set; }

        [Precision(18, 2)]
        public decimal PlatformFeeAmount { get; set; }

        [Precision(18, 2)]
        public decimal ShopAmount { get; set; }

        // ============ STATUS ============

        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        // ============ PAYMENT GATEWAY INFO (PayOS) ============

        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "PayOS";

        public long? PayOSOrderCode { get; set; }

        [MaxLength(255)]
        public string? PayOSTransactionId { get; set; }

        [MaxLength(1000)]
        public string? PayOSPaymentUrl { get; set; }

        // ============ TIMESTAMPS ============

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        // ============ METADATA ============

        [MaxLength(4000)]
        public string? PayOSMetadata { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        [MaxLength(50)]
        public string? VoucherCode { get; set; }
    }
}
