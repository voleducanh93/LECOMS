using LECOMS.Data.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Lịch sử giao dịch trong ShopWallet
    /// Mọi thay đổi số dư đều phải ghi log vào đây
    /// 
    /// MỤC ĐÍCH:
    /// 1. Audit trail - truy vết mọi giao dịch
    /// 2. Reconciliation - đối soát số dư
    /// 3. Reporting - báo cáo doanh thu, rút tiền
    /// 4. Dispute resolution - giải quyết tranh chấp
    /// </summary>
    [Index(nameof(ShopWalletId), nameof(CreatedAt))]
    [Index(nameof(Type))]
    [Index(nameof(ReferenceId))]
    public class WalletTransaction
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // ============ WALLET REFERENCE ============

        /// <summary>
        /// ID của ShopWallet
        /// </summary>
        [Required]
        public string ShopWalletId { get; set; } = null!;

        [ForeignKey(nameof(ShopWalletId))]
        public ShopWallet ShopWallet { get; set; } = null!;

        // ============ TRANSACTION INFO ============

        /// <summary>
        /// Loại giao dịch
        /// VD: OrderRevenue, Withdrawal, Refund, BalanceRelease
        /// </summary>
        public WalletTransactionType Type { get; set; }

        /// <summary>
        /// Số tiền thay đổi
        /// Dương (+): Cộng tiền (revenue, refund nhận)
        /// Âm (-): Trừ tiền (withdrawal, refund trả)
        /// VD: +950000 (nhận tiền order), -500000 (withdrawal)
        /// </summary>
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Số dư TRƯỚC khi giao dịch
        /// Dùng để verify tính toán
        /// </summary>
        [Precision(18, 2)]
        public decimal BalanceBefore { get; set; }

        /// <summary>
        /// Số dư SAU khi giao dịch
        /// = BalanceBefore + Amount
        /// Dùng để verify tính toán
        /// </summary>
        [Precision(18, 2)]
        public decimal BalanceAfter { get; set; }

        /// <summary>
        /// Balance type bị ảnh hưởng
        /// "Available" hoặc "Pending"
        /// </summary>
        [MaxLength(20)]
        public string BalanceType { get; set; } = "Available";

        // ============ DESCRIPTION ============

        /// <summary>
        /// Mô tả giao dịch (hiển thị cho shop)
        /// VD: "Doanh thu đơn hàng ORD20250106001"
        /// VD: "Rút tiền vào tài khoản VCB *1234"
        /// VD: "Hoàn tiền cho khách hàng - Đơn ORD20250106002"
        /// </summary>
        [Required, MaxLength(500)]
        public string Description { get; set; } = null!;

        // ============ REFERENCE ============

        /// <summary>
        /// ID tham chiếu đến entity liên quan
        /// VD: OrderId (nếu Type = OrderRevenue)
        /// VD: WithdrawalRequestId (nếu Type = Withdrawal)
        /// VD: RefundRequestId (nếu Type = Refund)
        /// Dùng để trace back nguồn gốc giao dịch
        /// </summary>
        [MaxLength(255)]
        public string? ReferenceId { get; set; }

        /// <summary>
        /// Loại reference
        /// VD: "Order", "WithdrawalRequest", "RefundRequest"
        /// </summary>
        [MaxLength(50)]
        public string? ReferenceType { get; set; }

        // ============ TIMESTAMP ============

        /// <summary>
        /// Thời gian thực hiện giao dịch
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ============ METADATA ============

        /// <summary>
        /// Admin thực hiện (nếu là manual adjustment)
        /// </summary>
        [MaxLength(255)]
        public string? PerformedBy { get; set; }

        /// <summary>
        /// Ghi chú thêm (internal note)
        /// </summary>
        [MaxLength(1000)]
        public string? Note { get; set; }
    }
}