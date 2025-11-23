using LECOMS.Data.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    [Table("RefundRequests")]
    [Index(nameof(OrderId))]
    [Index(nameof(Status))]
    [Index(nameof(RequestedBy))]
    public class RefundRequest
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // ============ ORDER ============

        [Required]
        public string OrderId { get; set; } = null!;

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        // ============ CUSTOMER REQUEST ============

        [Required]
        public string RequestedBy { get; set; } = null!;

        [ForeignKey(nameof(RequestedBy))]
        public User RequestedByUser { get; set; } = null!;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public RefundReason ReasonType { get; set; }

        [Required, MinLength(10), MaxLength(1000)]
        public string ReasonDescription { get; set; } = null!;

        public RefundType Type { get; set; }

        [Precision(18, 2)]
        public decimal RefundAmount { get; set; }

        public string? AttachmentUrls { get; set; }

        // ============ STATUS ============

        public RefundStatus Status { get; set; } = RefundStatus.PendingShop;

        // ============ SHOP RESPONSE ============

        public string? ShopResponseBy { get; set; }

        [ForeignKey(nameof(ShopResponseBy))]
        public User? ShopResponseByUser { get; set; }

        public DateTime? ShopRespondedAt { get; set; }

        public string? ShopRejectReason { get; set; }

        // ============ ADMIN RESPONSE ============

        public string? AdminResponseBy { get; set; }

        [ForeignKey(nameof(AdminResponseBy))]
        public User? AdminResponseByUser { get; set; }

        public DateTime? AdminRespondedAt { get; set; }

        public string? AdminRejectReason { get; set; }

        // ============ REFUND PROCESS ============

        public DateTime? RefundedAt { get; set; }

        public string? RefundTransactionId { get; set; } // PayOS hoặc Wallet

        public string? ProcessNote { get; set; }
    }
}
