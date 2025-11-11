using LECOMS.Data.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Yêu cầu rút tiền của Shop
    /// </summary>
    [Index(nameof(ShopWalletId))]
    [Index(nameof(Status))]
    [Index(nameof(RequestedAt))]
    public class WithdrawalRequest
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ShopWalletId { get; set; } = null!;

        [ForeignKey(nameof(ShopWalletId))]
        public ShopWallet ShopWallet { get; set; } = null!;

        // ⭐ SỬA: int thay vì string
        [Required]
        public int ShopId { get; set; }

        [ForeignKey(nameof(ShopId))]
        public Shop Shop { get; set; } = null!;

        [Precision(18, 2)]
        public decimal Amount { get; set; }

        [Required, MaxLength(100)]
        public string BankName { get; set; } = null!;

        [Required, MaxLength(50)]
        public string BankAccountNumber { get; set; } = null!;

        [Required, MaxLength(255)]
        public string BankAccountName { get; set; } = null!;

        [MaxLength(255)]
        public string? BankBranch { get; set; }

        public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;

        public string? ApprovedBy { get; set; }

        [ForeignKey(nameof(ApprovedBy))]
        public User? ApprovedByUser { get; set; }

        public DateTime? ApprovedAt { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime? ProcessedAt { get; set; }

        [MaxLength(255)]
        public string? TransactionReference { get; set; }

        public DateTime? CompletedAt { get; set; }

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Note { get; set; }

        [MaxLength(500)]
        public string? AdminNote { get; set; }
    }
}