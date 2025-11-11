using LECOMS.Data.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Entity lưu thông tin giao dịch thanh toán qua PayOS
    /// 
    /// ⭐ MÔ HÌNH SÀN THU HỘ (MARKETPLACE PAYMENT):
    /// - 1 Transaction có thể liên kết với NHIỀU Orders (từ nhiều shops khác nhau)
    /// - Customer chỉ thanh toán 1 LẦN DUY NHẤT cho toàn bộ giỏ hàng
    /// - Platform (sàn) nhận tiền trước, sau đó chia cho các shops
    /// - Platform giữ lại commission fee
    /// 
    /// FLOW:
    /// 1. Customer checkout cart có items từ nhiều shops
    ///    → Tạo nhiều Orders (1 order/shop) + 1 Transaction
    /// 2. Customer thanh toán qua PayOS 1 lần duy nhất
    ///    → PayOS webhook → Transaction.Status = Completed
    /// 3. Platform tự động chia tiền:
    ///    - Tính commission cho platform
    ///    - Cộng tiền vào ShopWallet.PendingBalance của mỗi shop
    /// 4. Sau holding period → Chuyển từ Pending sang Available
    /// 
    /// VÍ DỤ:
    /// Cart có 3 shops: Shop A (100k), Shop B (200k), Shop C (150k)
    /// → Total: 450k
    /// → Customer thanh toán 1 lần: 450k
    /// → Platform nhận 450k, giữ 5% commission (22.5k)
    /// → Chia cho shops: A=95k, B=190k, C=142.5k (vào PendingBalance)
    /// </summary>
    [Index(nameof(PayOSTransactionId), IsUnique = true)]
    [Index(nameof(PayOSOrderCode), IsUnique = true)]
    [Index(nameof(Status))]
    [Index(nameof(CreatedAt))]
    public class Transaction
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // ============ REFERENCE ============

        /// <summary>
        /// ⭐ Order IDs liên kết (có thể nhiều orders, comma-separated)
        /// Format: "orderId1,orderId2,orderId3"
        /// Example: "abc-123-def,ghi-456-jkl,mno-789-pqr"
        /// 
        /// ⚠️ KHÔNG CÓ navigation property Order
        /// Vì 1 transaction có thể map với nhiều orders
        /// Dùng string field để lưu danh sách OrderIds
        /// </summary>
        [Required]
        [MaxLength(4000)]
        public string OrderId { get; set; } = null!;

        // ❌ KHÔNG CÓ navigation property
        // public Order Order { get; set; }

        // ============ PAYMENT AMOUNTS ============

        /// <summary>
        /// Tổng số tiền customer phải thanh toán
        /// = Tổng của TẤT CẢ orders + shipping fee - discount
        /// VD: Order1(100k) + Order2(200k) + Order3(150k) + Ship(30k) - Discount(0) = 480k
        /// </summary>
        [Precision(18, 2)]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// % Phí sàn (commission rate)
        /// Lấy từ PlatformConfig.DefaultCommissionRate
        /// VD: 5.00 (nghĩa là 5%)
        /// </summary>
        [Precision(5, 2)]
        public decimal PlatformFeePercent { get; set; }

        /// <summary>
        /// Số tiền phí sàn platform giữ lại
        /// = TotalAmount * PlatformFeePercent / 100
        /// VD: 480,000 * 5 / 100 = 24,000 VND
        /// Đây là doanh thu của platform
        /// </summary>
        [Precision(18, 2)]
        public decimal PlatformFeeAmount { get; set; }

        /// <summary>
        /// Tổng số tiền TẤT CẢ shops thực nhận
        /// = TotalAmount - PlatformFeeAmount
        /// VD: 480,000 - 24,000 = 456,000 VND
        /// 
        /// Số tiền này sẽ được chia cho các shops theo tỷ lệ từng order:
        /// - Shop A (Order1=100k): 95,000 VND (100k - 5%)
        /// - Shop B (Order2=200k): 190,000 VND (200k - 5%)
        /// - Shop C (Order3=150k): 142,500 VND (150k - 5%)
        /// - Shipping: 28,500 VND (30k - 5%)
        /// Total: 456,000 VND ✅
        /// </summary>
        [Precision(18, 2)]
        public decimal ShopAmount { get; set; }

        // ============ STATUS ============

        /// <summary>
        /// Trạng thái transaction
        /// - Pending: Chờ thanh toán
        /// - Completed: Đã thanh toán thành công
        /// - Failed: Thanh toán thất bại
        /// - Cancelled: Đã hủy
        /// - Refunded: Đã hoàn tiền
        /// </summary>
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        // ============ PAYMENT GATEWAY INFO (PayOS) ============

        /// <summary>
        /// Payment method - luôn là PayOS
        /// </summary>
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "PayOS";

        /// <summary>
        /// OrderCode gửi cho PayOS (số nguyên dương)
        /// Generated từ Transaction.Id
        /// PayOS trả về code này trong webhook để map lại transaction
        /// VD: 1234567890
        /// </summary>
        public long? PayOSOrderCode { get; set; }

        /// <summary>
        /// Transaction ID từ PayOS (PaymentLinkId)
        /// Nhận được từ response khi tạo payment link
        /// Dùng để tracking, reconciliation, refund
        /// </summary>
        [MaxLength(255)]
        public string? PayOSTransactionId { get; set; }

        /// <summary>
        /// Payment URL từ PayOS
        /// URL để redirect customer đi thanh toán
        /// VD: https://pay.payos.vn/web/abc123xyz
        /// </summary>
        [MaxLength(1000)]
        public string? PayOSPaymentUrl { get; set; }

        // ============ TIMESTAMPS ============

        /// <summary>
        /// Thời gian tạo transaction
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời gian thanh toán thành công
        /// Set khi nhận webhook từ PayOS với code "00"
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        // ============ METADATA ============

        /// <summary>
        /// Response data từ PayOS (JSON string)
        /// Lưu toàn bộ webhook data để audit, debug, reconciliation
        /// </summary>
        [MaxLength(4000)]
        public string? PayOSMetadata { get; set; }

        /// <summary>
        /// Ghi chú thêm
        /// </summary>
        [MaxLength(500)]
        public string? Note { get; set; }
    }
}