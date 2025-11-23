using LECOMS.Data.Enum;

namespace LECOMS.Data.DTOs.Refund
{
    /// <summary>
    /// DTO để Customer tạo yêu cầu hoàn tiền
    /// </summary>
    public class CreateRefundRequestDTO
    {
        /// <summary>
        /// Id đơn hàng cần hoàn tiền
        /// </summary>
        public string OrderId { get; set; } = null!;

        /// <summary>
        /// Lý do refund (enum)
        /// </summary>
        public RefundReason ReasonType { get; set; }

        /// <summary>
        /// Mô tả chi tiết lý do (10 - 1000 ký tự)
        /// </summary>
        public string ReasonDescription { get; set; } = null!;

        /// <summary>
        /// Loại refund: Full / Partial
        /// </summary>
        public RefundType Type { get; set; }

        /// <summary>
        /// Số tiền cần refund (nếu Partial)
        /// </summary>
        public decimal RefundAmount { get; set; }

        /// <summary>
        /// Link ảnh/video chứng minh lỗi (optional)
        /// </summary>
        public string? AttachmentUrls { get; set; }
    }
}
