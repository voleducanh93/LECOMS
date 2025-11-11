using LECOMS.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    /// <summary>
    /// Repository cho CustomerWallet
    /// </summary>
    public interface ICustomerWalletRepository : IRepository<CustomerWallet>
    {
        /// <summary>
        /// Lấy wallet theo CustomerId
        /// Relationship 1-1: 1 customer có 1 wallet duy nhất
        /// </summary>
        Task<CustomerWallet?> GetByCustomerIdAsync(string customerId, bool includeTransactions = false);

        /// <summary>
        /// Lấy wallet với transactions (eager loading)
        /// </summary>
        Task<CustomerWallet?> GetByIdWithTransactionsAsync(string walletId);

        /// <summary>
        /// Kiểm tra customer có đủ balance không
        /// </summary>
        Task<bool> HasSufficientBalanceAsync(string customerId, decimal amount);

        /// <summary>
        /// Lấy danh sách customers có balance > 0
        /// Dùng cho admin report
        /// </summary>
        Task<IEnumerable<CustomerWallet>> GetWalletsWithBalanceAsync(int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Lấy tổng balance của tất cả customers
        /// </summary>
        Task<decimal> GetTotalBalanceAsync();
    }
}