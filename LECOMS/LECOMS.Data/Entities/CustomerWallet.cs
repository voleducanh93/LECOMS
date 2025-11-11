using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Ví của Customer - chứa tiền từ refund
    /// 
    /// USE CASES:
    /// 1. Nhận tiền refund khi shop có vấn đề
    /// 2. Dùng số dư để thanh toán đơn hàng mới (optional feature)
    /// 3. Rút tiền về bank account
    /// 
    /// MỖI CUSTOMER CÓ 1 WALLET DUY NHẤT
    /// </summary>
    [Index(nameof(CustomerId), IsUnique = true)]
    public class CustomerWallet
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // ============ OWNER ============

        /// <summary>
        /// ID của customer sở hữu ví
        /// Quan hệ 1-1 với User (role Customer)
        /// </summary>
        [Required]
        public string CustomerId { get; set; } = null!;

        [ForeignKey(nameof(CustomerId))]
        public User Customer { get; set; } = null!;

        // ============ BALANCE ============

        /// <summary>
        /// Số dư hiện tại
        /// 
        /// TĂNG: Khi nhận refund từ shop issue
        /// GIẢM: Khi thanh toán đơn hàng (optional), rút tiền, hoặc refund to shop
        /// 
        /// Customer có thể:
        /// 1. Dùng để thanh toán đơn hàng mới (feature tương lai)
        /// 2. Rút về bank account (tương tự shop withdrawal)
        /// </summary>
        [Precision(18, 2)]
        public decimal Balance { get; set; } = 0;

        // ============ STATISTICS (Read-only, for reporting) ============

        /// <summary>
        /// Tổng tiền đã nhận từ refund
        /// Chỉ tăng, không giảm
        /// </summary>
        [Precision(18, 2)]
        public decimal TotalRefunded { get; set; } = 0;

        /// <summary>
        /// Tổng tiền đã chi tiêu từ ví
        /// VD: Dùng balance để mua hàng
        /// Chỉ tăng, không giảm
        /// </summary>
        [Precision(18, 2)]
        public decimal TotalSpent { get; set; } = 0;

        /// <summary>
        /// Tổng tiền đã rút về bank
        /// Chỉ tăng, không giảm
        /// </summary>
        [Precision(18, 2)]
        public decimal TotalWithdrawn { get; set; } = 0;

        // ============ TIMESTAMPS ============

        /// <summary>
        /// Lần cập nhật số dư cuối cùng
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Ngày tạo ví
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ============ NAVIGATION PROPERTIES ============

        /// <summary>
        /// Lịch sử giao dịch ví
        /// Audit trail
        /// </summary>
        public ICollection<CustomerWalletTransaction> Transactions { get; set; } = new List<CustomerWalletTransaction>();

        /// <summary>
        /// Danh sách yêu cầu rút tiền (optional feature)
        /// </summary>
        public ICollection<CustomerWithdrawalRequest> WithdrawalRequests { get; set; } = new List<CustomerWithdrawalRequest>();
    }
}