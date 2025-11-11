using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    /// <summary>
    /// Service quản lý CustomerWallet
    /// </summary>
    public interface ICustomerWalletService
    {
        /// <summary>
        /// Lấy hoặc tạo mới wallet cho customer
        /// </summary>
        Task<CustomerWallet> GetOrCreateWalletAsync(string customerId);

        /// <summary>
        /// Lấy wallet với transactions
        /// </summary>
        Task<CustomerWallet?> GetWalletWithTransactionsAsync(string customerId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Cộng tiền vào wallet
        /// Dùng khi: Refund from shop
        /// 
        /// FLOW:
        /// 1. Lấy wallet (hoặc tạo mới)
        /// 2. Cộng amount vào Balance
        /// 3. Cộng amount vào TotalRefunded
        /// 4. Ghi log CustomerWalletTransaction
        /// </summary>
        Task<CustomerWallet> AddBalanceAsync(string customerId, decimal amount, string refundId, string description);

        /// <summary>
        /// Trừ tiền từ wallet
        /// Dùng khi:
        /// - Customer thanh toán bằng wallet balance
        /// - Refund to shop (customer hủy đơn)
        /// - Withdrawal
        /// </summary>
        Task<CustomerWallet> DeductBalanceAsync(
            string customerId,
            decimal amount,
            WalletTransactionType type,
            string referenceId,
            string description);

        /// <summary>
        /// Kiểm tra customer có đủ balance không
        /// </summary>
        Task<bool> HasSufficientBalanceAsync(string customerId, decimal amount);

        /// <summary>
        /// Lấy balance hiện tại
        /// </summary>
        Task<decimal> GetBalanceAsync(string customerId);
    }
}