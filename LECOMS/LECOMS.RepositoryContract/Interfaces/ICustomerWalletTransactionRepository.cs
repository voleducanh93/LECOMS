using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    /// <summary>
    /// Repository cho CustomerWalletTransaction
    /// </summary>
    public interface ICustomerWalletTransactionRepository : IRepository<CustomerWalletTransaction>
    {
        /// <summary>
        /// Lấy transactions theo CustomerWalletId
        /// </summary>
        Task<IEnumerable<CustomerWalletTransaction>> GetByWalletIdAsync(
            string customerWalletId,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Lấy transactions theo type
        /// </summary>
        Task<IEnumerable<CustomerWalletTransaction>> GetByTypeAsync(
            string customerWalletId,
            WalletTransactionType type,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Lấy transactions trong khoảng thời gian
        /// </summary>
        Task<IEnumerable<CustomerWalletTransaction>> GetByDateRangeAsync(
            string customerWalletId,
            DateTime fromDate,
            DateTime toDate);

        /// <summary>
        /// Lấy transaction theo ReferenceId
        /// </summary>
        Task<CustomerWalletTransaction?> GetByReferenceIdAsync(string referenceId);
    }
}