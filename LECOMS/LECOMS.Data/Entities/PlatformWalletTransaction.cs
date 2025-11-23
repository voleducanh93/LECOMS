using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Giao dịch của ví sàn (log kế toán)
    /// </summary>
    public class PlatformWalletTransaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string PlatformWalletId { get; set; }
        public PlatformWallet PlatformWallet { get; set; }

        /// <summary>
        /// Số tiền giao dịch
        /// > 0: tiền vào ví sàn
        /// < 0: tiền ra khỏi ví sàn
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Loại giao dịch (CommissionIncome, CommissionRefund,...)
        /// </summary>
        public PlatformWalletTransactionType Type { get; set; }

        /// <summary>
        /// Số dư trước giao dịch
        /// </summary>
        public decimal BalanceBefore { get; set; }

        /// <summary>
        /// Số dư sau giao dịch
        /// </summary>
        public decimal BalanceAfter { get; set; }

        /// <summary>
        /// Id của entity gốc (OrderId / RefundId / TransactionId / WithdrawalId / v.v)
        /// </summary>
        public string ReferenceId { get; set; }

        /// <summary>
        /// Loại reference: "Order", "Refund", "Transaction", "Payout", "Manual"
        /// </summary>
        public string ReferenceType { get; set; }

        /// <summary>
        /// Mô tả chi tiết cho FE hiển thị
        /// </summary>
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
