using LECOMS.Data.Entities;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    /// <summary>
    /// Repository cho ShopWallet
    /// </summary>
    public interface IShopWalletRepository : IRepository<ShopWallet>
    {
        /// <summary>
        /// Lấy wallet theo ShopId
        /// Relationship 1-1: 1 shop có 1 wallet duy nhất
        /// </summary>
        Task<ShopWallet?> GetByShopIdAsync(int shopId, bool includeTransactions = false);

        /// <summary>
        /// Lấy wallet với transactions (eager loading)
        /// </summary>
        Task<ShopWallet?> GetByIdWithTransactionsAsync(string walletId);

        /// <summary>
        /// Lấy wallet với Yêu cầu rút tiền
        /// </summary>
        Task<ShopWallet?> GetByIdWithWithdrawalsAsync(string walletId);

        /// <summary>
        /// Kiểm tra shop có đủ balance để rút tiền không
        /// </summary>
        Task<bool> HasSufficientBalanceAsync(int shopId, decimal amount);

        /// <summary>
        /// Lấy tổng balance của tất cả shops (cho admin report)
        /// </summary>
        Task<decimal> GetTotalAvailableBalanceAsync();

        /// <summary>
        /// Lấy tổng pending balance của tất cả shops
        /// </summary>
        Task<decimal> GetTotalPendingBalanceAsync();
    }
}