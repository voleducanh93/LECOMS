using LECOMS.Data.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Yêu cầu hoàn tiền - MANUAL REFUND TO WALLET
    /// 
    /// FLOW:
    /// 1. Customer/Shop tạo request → Status = Pending
    /// 2. Admin review → Approve/Reject
    /// 3. Nếu Approve → Status = Processing
    ///    - Cộng tiền vào Recipient Wallet (Customer hoặc Shop)
    ///    - Trừ tiền từ bên còn lại
    /// 4. Status = Completed
    /// 
    /// LƯU Ý: KHÔNG dùng PayOS refund API
    /// Tất cả refund đều vào ví nội bộ (manual)
    /// </summary>
    [Index(nameof(OrderId))]
    [Index(nameof(Status))]
    [Index(nameof(RequestedBy))]
    [Index(nameof(RequestedAt))]
    public class RefundRequest
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // ============ ORDER REFERENCE ============

        /// <summary>
        /// ID của order cần hoàn tiền
        /// </summary>
        [Required]
        public string OrderId { get; set; } = null!;

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        // ============ REQUESTER INFO ============

        /// <summary>
        /// Người tạo request
        /// Có thể là Customer hoặc Shop
        /// </summary>
        [Required]
        public string RequestedBy { get; set; } = null!;

        [ForeignKey(nameof(RequestedBy))]
        public User RequestedByUser { get; set; } = null!;

        // ============ REFUND INFO ============

        /// <summary>
        /// Người nhận tiền hoàn
        /// Customer: Tiền vào CustomerWallet
        /// Shop: Tiền vào ShopWallet
        /// 
        /// Tự động xác định từ ReasonType
        /// Hoặc admin chọn manual nếu ReasonType = Other
        /// </summary>
        public RefundRecipient Recipient { get; set; }

        /// <summary>
        /// Loại lý do (Enum) - cho filtering/reporting
        /// VD: ShopIssue, CustomerCancelled
        /// </summary>
        public RefundReason ReasonType { get; set; }

        /// <summary>
        /// Mô tả chi tiết lý do (BẮT BUỘC, tối thiểu 10 ký tự)
        /// VD: "Sản phẩm bị vỡ khi giao hàng, có hình ảnh đính kèm"
        /// VD: "Khách hàng không nghe máy, từ chối nhận hàng 3 lần"
        /// 
        /// QUAN TRỌNG: Đây là thông tin chính cho admin review
        /// </summary>
        [Required, MinLength(10), MaxLength(1000)]
        public string ReasonDescription { get; set; } = null!;

        /// <summary>
        /// Full refund hay partial
        /// </summary>
        public RefundType Type { get; set; }

        /// <summary>
        /// Số tiền hoàn
        /// - Full: = Order.Total
        /// - Partial: < Order.Total
        /// </summary>
        [Precision(18, 2)]
        public decimal RefundAmount { get; set; }

        // ============ STATUS ============

        /// <summary>
        /// Trạng thái request
        /// Pending → Approved/Rejected → Processing → Completed/Failed
        /// </summary>
        public RefundStatus Status { get; set; } = RefundStatus.Pending;

        // ============ APPROVAL INFO ============

        /// <summary>
        /// Admin xử lý request
        /// </summary>
        public string? ProcessedBy { get; set; }

        [ForeignKey(nameof(ProcessedBy))]
        public User? ProcessedByUser { get; set; }

        /// <summary>
        /// Ghi chú của admin khi approve/reject
        /// VD khi approve: "Đã xác minh hình ảnh, hàng thực sự bị lỗi"
        /// VD khi reject: "Không đủ bằng chứng, customer đã sử dụng sản phẩm"
        /// </summary>
        [MaxLength(500)]
        public string? ProcessNote { get; set; }

        // ============ TIMESTAMPS ============

        /// <summary>
        /// Thời gian tạo request
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời gian admin xử lý (approve/reject)
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        // ============ ATTACHMENTS (Optional) ============

        /// <summary>
        /// URL hình ảnh/video chứng minh (nếu có)
        /// Lưu dạng JSON array: ["url1", "url2", "url3"]
        /// VD: Hình ảnh hàng lỗi, video unboxing
        /// </summary>
        [MaxLength(2000)]
        public string? AttachmentUrls { get; set; }
    }
}