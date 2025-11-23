using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Enum
{
    /// <summary>
    /// Loại giao dịch của ví sàn (PlatformWallet)
    /// </summary>
    public enum PlatformWalletTransactionType
    {
        /// <summary>
        /// Hoa hồng sàn thu được từ đơn hàng
        /// </summary>
        CommissionIncome = 0,

        /// <summary>
        /// Hoàn trả hoa hồng khi refund đơn
        /// </summary>
        CommissionRefund = 1,

        /// <summary>
        /// Rút tiền từ ví sàn về tài khoản ngân hàng / ví ngoài
        /// </summary>
        PayoutToBank = 2,

        /// <summary>
        /// Điều chỉnh thủ công (admin cộng / trừ)
        /// </summary>
        ManualAdjust = 3,

        /// <summary>
        /// Topup / nạp vào ví sàn (ít dùng, chủ yếu cho test)
        /// </summary>
        Topup = 4
    }
}
