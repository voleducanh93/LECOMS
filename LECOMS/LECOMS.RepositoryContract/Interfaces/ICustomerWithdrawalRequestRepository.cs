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
        Task<IEnumerable<CustomerWithdrawalRequest>> GetByCustomerIdAsync(string customerId, int pageNumber, int pageSize);
        Task<IEnumerable<CustomerWithdrawalRequest>> GetPendingAsync();
        Task<CustomerWithdrawalRequest?> GetByIdWithDetailsAsync(string id);
    }
}