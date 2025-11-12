// Path: LECOMS/LECOMS.Data/Entities/RefundRequest.cs
using LECOMS.Data.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Yêu cầu hoàn tiền từ Customer
    /// </summary>
    [Table("RefundRequests")]
    [Index(nameof(OrderId))]
    [Index(nameof(Status))]
    [Index(nameof(RequestedBy))]
    [Index(nameof(RequestedAt))]
    public class RefundRequest
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // ============ ORDER & CUSTOMER ============

        [Required]
        public string OrderId { get; set; } = null!;

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        [Required]
        public string RequestedBy { get; set; } = null!;

        [ForeignKey(nameof(RequestedBy))]
        public User RequestedByUser { get; set; } = null!;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // ============ REFUND INFO ============

        public RefundReason ReasonType { get; set; }

        [Required, MinLength(10), MaxLength(1000)]
        public string ReasonDescription { get; set; } = null!;

        public RefundType Type { get; set; }

        [Precision(18, 2)]
        public decimal RefundAmount { get; set; }

        [MaxLength(2000)]
        public string? AttachmentUrls { get; set; }

        // ============ STATUS ============

        public RefundStatus Status { get; set; } = RefundStatus.PendingShopApproval;

        // ============ SHOP RESPONSE ============

        public string? ShopResponseBy { get; set; }

        [ForeignKey(nameof(ShopResponseBy))]
        public User? ShopResponseByUser { get; set; }

        public DateTime? ShopRespondedAt { get; set; }

        [MaxLength(500)]
        public string? ShopRejectReason { get; set; }

        // ============ PROCESSING ============

        public DateTime? ProcessedAt { get; set; }

        [MaxLength(500)]
        public string? ProcessNote { get; set; }

        // ============ FRAUD DETECTION ============

        public bool IsFlagged { get; set; }

        [MaxLength(200)]
        public string? FlagReason { get; set; }
    }
}