using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    /// <summary>
    /// Repository cho CustomerWithdrawalRequest
    /// </summary>
    public interface ICustomerWithdrawalRequestRepository : IRepository<CustomerWithdrawalRequest>
    {
        /// <summary>
        /// Lấy withdrawal requests theo CustomerId
        /// </summary>
        Task<IEnumerable<CustomerWithdrawalRequest>> GetByCustomerIdAsync(
            string customerId,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Lấy withdrawal request với Customer details
        /// </summary>
        Task<CustomerWithdrawalRequest?> GetByIdWithDetailsAsync(string withdrawalId);

        /// <summary>
        /// Lấy withdrawal requests theo status
        /// </summary>
        Task<IEnumerable<CustomerWithdrawalRequest>> GetByStatusAsync(
            WithdrawalStatus status,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Lấy pending withdrawal requests
        /// </summary>
        Task<IEnumerable<CustomerWithdrawalRequest>> GetPendingRequestsAsync();

        /// <summary>
        /// Lấy approved withdrawals (chờ processing)
        /// </summary>
        Task<IEnumerable<CustomerWithdrawalRequest>> GetApprovedRequestsAsync();
    }
}