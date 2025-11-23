using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Ví sàn (Platform Wallet) - lưu số dư của nền tảng LECOMS
    /// </summary>
    public class PlatformWallet
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Tổng số dư hiện tại của ví sàn
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Tổng hoa hồng đã thu được từ trước đến nay
        /// </summary>
        public decimal TotalCommissionEarned { get; set; }

        /// <summary>
        /// Tổng hoa hồng đã hoàn trả (khi refund đơn)
        /// </summary>
        public decimal TotalCommissionRefunded { get; set; }

        /// <summary>
        /// Tổng tiền đã rút ra khỏi ví sàn (payout)
        /// </summary>
        public decimal TotalPayout { get; set; }

        /// <summary>
        /// Ngày tạo ví
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Lần cập nhật cuối cùng
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public ICollection<PlatformWalletTransaction> Transactions { get; set; }
            = new List<PlatformWalletTransaction>();
    }
}
