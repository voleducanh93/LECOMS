using LECOMS.Data.DTOs.Wallet;
using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IPlatformWalletService
    {
        /// <summary>
        /// Lấy (hoặc tạo) ví sàn singleton
        /// </summary>
        Task<PlatformWallet> GetOrCreateAsync();

        /// <summary>
        /// Ghi nhận hoa hồng sàn khi đơn thanh toán thành công
        /// </summary>
        Task<PlatformWallet> AddCommissionAsync(
            decimal commissionAmount,
            string transactionId,
            string orderCodesDescription);

        /// <summary>
        /// Hoàn trả hoa hồng khi refund (full hoặc partial)
        /// </summary>
        Task<PlatformWallet> RefundCommissionAsync(
            decimal commissionRefundAmount,
            string refundId,
            string description);

        /// <summary>
        /// Rút tiền từ ví sàn (payout) về tài khoản ngoài
        /// </summary>
        Task<PlatformWallet> PayoutAsync(
            decimal amount,
            string payoutId,
            string description);

        /// <summary>
        /// Điều chỉnh thủ công (admin)
        /// </summary>
        Task<PlatformWallet> ManualAdjustAsync(
            decimal amount,
            string description);

        /// <summary>
        /// Dashboard ví sàn
        /// </summary>
        Task<PlatformWalletDTO> GetSummaryAsync();

        /// <summary>
        /// Lịch sử giao dịch ví sàn
        /// </summary>
        Task<IEnumerable<PlatformWalletTransactionDTO>> GetTransactionsAsync(
            int pageNumber = 1,
            int pageSize = 50);
    }
}
