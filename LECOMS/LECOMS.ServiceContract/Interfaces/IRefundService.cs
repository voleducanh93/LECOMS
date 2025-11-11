using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    /// <summary>
    /// Service xử lý hoàn tiền - MANUAL REFUND TO WALLET
    /// 
    /// QUAN TRỌNG:
    /// - KHÔNG dùng PayOS refund API
    /// - TẤT CẢ refund đều vào ví nội bộ
    /// - BẮT BUỘC admin approval
    /// </summary>
    public interface IRefundService
    {
        /// <summary>
        /// Tạo refund request
        /// 
        /// FLOW:
        /// 1. Validate order (phải đã thanh toán, chưa refund toàn bộ)
        /// 2. Validate amount (không vượt quá order total - đã refund)
        /// 3. Auto-determine Recipient từ ReasonType
        /// 4. Tạo RefundRequest (Status = Pending)
        /// 5. Send notification cho admin
        /// </summary>
        Task<RefundRequest> CreateRefundRequestAsync(CreateRefundRequestDto dto);

        /// <summary>
        /// Admin approve/reject refund request
        /// 
        /// FLOW KHI APPROVE:
        /// 1. Update RefundRequest.Status = Approved
        /// 2. Update RefundRequest.ProcessedBy = adminId
        /// 3. Trigger ProcessRefundAsync()
        /// 
        /// FLOW KHI REJECT:
        /// 1. Update RefundRequest.Status = Rejected
        /// 2. Update RefundRequest.ProcessNote
        /// 3. Send notification cho requester
        /// </summary>
        Task<RefundRequest> ApproveRefundAsync(string refundId, string adminId, string? note = null);
        Task<RefundRequest> RejectRefundAsync(string refundId, string adminId, string reason);

        /// <summary>
        /// Xử lý refund (CORE LOGIC)
        /// 
        /// FLOW REFUND TO CUSTOMER (ShopIssue, ShopCancelled):
        /// 1. BEGIN TRANSACTION
        /// 2. Cộng tiền vào CustomerWallet
        /// 3. Trừ tiền từ ShopWallet (ưu tiên Available, sau đó Pending)
        ///    - Nếu không đủ → Tạo ShopDebt (optional)
        /// 4. Platform GIỮ LẠI phí sàn (đã cung cấp dịch vụ)
        /// 5. Update Transaction.Status = Refunded/PartiallyRefunded
        /// 6. Update Order.PaymentStatus
        /// 7. Update RefundRequest.Status = Completed
        /// 8. Ghi log WalletTransaction (cả 2 bên)
        /// 9. COMMIT TRANSACTION
        /// 10. Send notifications
        /// 
        /// FLOW REFUND TO SHOP (CustomerCancelled):
        /// 1. BEGIN TRANSACTION
        /// 2. Cộng tiền vào ShopWallet (TOÀN BỘ, bao gồm phí sàn)
        /// 3. Trừ tiền từ CustomerWallet
        ///    - Nếu không đủ → Tạo CustomerDebt (optional)
        /// 4. Platform HOÀN LẠI phí sàn cho shop
        /// 5. Update Transaction.Status = Refunded
        /// 6. Update Order.PaymentStatus = Refunded
        /// 7. Update Order.Status = Cancelled
        /// 8. Update RefundRequest.Status = Completed
        /// 9. Ghi log WalletTransaction
        /// 10. COMMIT TRANSACTION
        /// 11. Send notifications
        /// </summary>
        Task<RefundRequest> ProcessRefundAsync(string refundId);

        /// <summary>
        /// Lấy refund request by ID
        /// </summary>
        Task<RefundRequest?> GetRefundRequestAsync(string refundId);

        /// <summary>
        /// Lấy refund requests theo order
        /// </summary>
        Task<IEnumerable<RefundRequest>> GetRefundRequestsByOrderAsync(string orderId);

        /// <summary>
        /// Lấy pending refund requests (cho admin dashboard)
        /// </summary>
        Task<IEnumerable<RefundRequest>> GetPendingRefundRequestsAsync();

        /// <summary>
        /// Lấy refund requests theo user
        /// </summary>
        Task<IEnumerable<RefundRequest>> GetRefundRequestsByUserAsync(string userId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Kiểm tra order có thể refund không
        /// </summary>
        Task<bool> CanRefundOrderAsync(string orderId);

        /// <summary>
        /// Tính số tiền có thể refund (order total - đã refund)
        /// </summary>
        Task<decimal> GetRefundableAmountAsync(string orderId);
    }

    /// <summary>
    /// DTO để tạo refund request
    /// </summary>
    public class CreateRefundRequestDto
    {
        public string OrderId { get; set; } = null!;
        public string RequestedBy { get; set; } = null!;
        public RefundReason ReasonType { get; set; }
        public string ReasonDescription { get; set; } = null!;
        public RefundType Type { get; set; }
        public decimal RefundAmount { get; set; }
        public string? AttachmentUrls { get; set; }
    }
}