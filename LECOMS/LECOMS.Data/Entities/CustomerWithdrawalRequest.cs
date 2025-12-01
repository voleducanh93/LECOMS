using LECOMS.Data.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Yêu cầu rút tiền từ CustomerWallet về tài khoản ngân hàng
    /// Customer có thể là buyer, hoặc seller cũng có ví cá nhân.
    /// </summary>
    [Index(nameof(CustomerWalletId))]
    [Index(nameof(Status))]
    [Index(nameof(RequestedAt))]
    public class CustomerWithdrawalRequest
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CustomerWalletId { get; set; } = null!;

        [ForeignKey(nameof(CustomerWalletId))]
        public CustomerWallet CustomerWallet { get; set; } = null!;

        [Required]
        public string CustomerId { get; set; } = null!;

        [ForeignKey(nameof(CustomerId))]
        public User Customer { get; set; } = null!;

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
        public DateTime? CompletedAt { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Note { get; set; }

        [MaxLength(500)]
        public string? AdminNote { get; set; }
    }
}