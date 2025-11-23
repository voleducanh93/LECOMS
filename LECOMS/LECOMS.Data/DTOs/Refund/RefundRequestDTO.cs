using LECOMS.Data.Enum;

namespace LECOMS.Data.DTOs.Refund
{
    /// <summary>
    /// DTO hiển thị thông tin Refund Request cho Customer / Seller / Admin
    /// </summary>
    public class RefundRequestDTO
    {
        public string Id { get; set; } = null!;

        // =========================
        // ORDER INFO
        // =========================
        public string OrderId { get; set; } = null!;
        public string? OrderCode { get; set; }

        // =========================
        // CUSTOMER INFO
        // =========================
        public string RequestedBy { get; set; } = null!;
        public string? RequestedByName { get; set; }
        public DateTime RequestedAt { get; set; }

        // =========================
        // REFUND DETAILS
        // =========================
        public RefundReason ReasonType { get; set; }
        public string ReasonDescription { get; set; } = null!;
        public RefundType Type { get; set; }
        public decimal RefundAmount { get; set; }
        public string? AttachmentUrls { get; set; }

        // =========================
        // STATUS
        // =========================
        public RefundStatus Status { get; set; }

        // =========================
        // SHOP RESPONSE
        // =========================
        public string? ShopResponseBy { get; set; }
        public string? ShopResponseByName { get; set; }
        public DateTime? ShopRespondedAt { get; set; }
        public string? ShopRejectReason { get; set; }

        // =========================
        // ADMIN PROCESSING
        // =========================
        public string? ProcessedBy { get; set; }
        public string? ProcessedByName { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessNote { get; set; }

        
    }
}
