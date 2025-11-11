using LECOMS.Data.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Lịch sử giao dịch trong CustomerWallet
    /// Tương tự WalletTransaction nhưng cho customer
    /// </summary>
    [Index(nameof(CustomerWalletId), nameof(CreatedAt))]
    [Index(nameof(Type))]
    [Index(nameof(ReferenceId))]
    public class CustomerWalletTransaction
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // ============ WALLET REFERENCE ============

        [Required]
        public string CustomerWalletId { get; set; } = null!;

        [ForeignKey(nameof(CustomerWalletId))]
        public CustomerWallet CustomerWallet { get; set; } = null!;

        // ============ TRANSACTION INFO ============

        /// <summary>
        /// Loại giao dịch
        /// VD: Refund (nhận tiền), Payment (mua hàng), Withdrawal (rút tiền)
        /// </summary>
        public WalletTransactionType Type { get; set; }

        /// <summary>
        /// Số tiền thay đổi
        /// Dương: Cộng tiền | Âm: Trừ tiền
        /// </summary>
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Số dư trước giao dịch
        /// </summary>
        [Precision(18, 2)]
        public decimal BalanceBefore { get; set; }

        /// <summary>
        /// Số dư sau giao dịch
        /// </summary>
        [Precision(18, 2)]
        public decimal BalanceAfter { get; set; }

        // ============ DESCRIPTION ============

        /// <summary>
        /// Mô tả giao dịch
        /// VD: "Hoàn tiền đơn hàng ORD20250106001 - Hàng lỗi"
        /// </summary>
        [Required, MaxLength(500)]
        public string Description { get; set; } = null!;

        // ============ REFERENCE ============

        /// <summary>
        /// ID tham chiếu
        /// VD: RefundRequestId, OrderId, WithdrawalRequestId
        /// </summary>
        [MaxLength(255)]
        public string? ReferenceId { get; set; }

        [MaxLength(50)]
        public string? ReferenceType { get; set; }

        // ============ TIMESTAMP ============

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ============ METADATA ============

        [MaxLength(255)]
        public string? PerformedBy { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }
    }
}