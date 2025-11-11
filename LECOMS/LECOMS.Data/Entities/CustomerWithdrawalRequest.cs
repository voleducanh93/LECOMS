using LECOMS.Data.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Yêu cầu rút tiền từ CustomerWallet về bank account
    /// Tương tự WithdrawalRequest nhưng cho customer
    /// 
    /// USE CASE: Customer nhận refund vào ví → muốn rút về bank
    /// </summary>
    [Index(nameof(CustomerWalletId))]
    [Index(nameof(Status))]
    [Index(nameof(RequestedAt))]
    public class CustomerWithdrawalRequest
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // ============ WALLET REFERENCE ============

        [Required]
        public string CustomerWalletId { get; set; } = null!;

        [ForeignKey(nameof(CustomerWalletId))]
        public CustomerWallet CustomerWallet { get; set; } = null!;

        [Required]
        public string CustomerId { get; set; } = null!;

        [ForeignKey(nameof(CustomerId))]
        public User Customer { get; set; } = null!;

        // ============ AMOUNT ============

        /// <summary>
        /// Số tiền muốn rút
        /// Phải <= CustomerWallet.Balance
        /// </summary>
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        // ============ BANK INFO ============

        [Required, MaxLength(100)]
        public string BankName { get; set; } = null!;

        [Required, MaxLength(50)]
        public string BankAccountNumber { get; set; } = null!;

        [Required, MaxLength(255)]
        public string BankAccountName { get; set; } = null!;

        [MaxLength(255)]
        public string? BankBranch { get; set; }

        // ============ STATUS ============

        public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;

        // ============ APPROVAL INFO ============

        public string? ApprovedBy { get; set; }

        [ForeignKey(nameof(ApprovedBy))]
        public User? ApprovedByUser { get; set; }

        public DateTime? ApprovedAt { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        // ============ PROCESSING INFO ============

        public DateTime? ProcessedAt { get; set; }

        [MaxLength(255)]
        public string? TransactionReference { get; set; }

        public DateTime? CompletedAt { get; set; }

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        // ============ TIMESTAMPS ============

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // ============ NOTES ============

        [MaxLength(500)]
        public string? Note { get; set; }

        [MaxLength(500)]
        public string? AdminNote { get; set; }
    }
}