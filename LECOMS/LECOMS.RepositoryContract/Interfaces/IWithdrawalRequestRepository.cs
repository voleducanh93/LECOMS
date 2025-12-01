using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    /// <summary>
    /// Repository cho WithdrawalRequest (Shop)
    /// </summary>
    public interface IWithdrawalRequestRepository : IRepository<WithdrawalRequest>
    {
        Task<IEnumerable<WithdrawalRequest>> GetByShopIdAsync(int shopId, int pageNumber, int pageSize);
        Task<IEnumerable<WithdrawalRequest>> GetPendingAsync();
        Task<WithdrawalRequest?> GetByIdWithDetailsAsync(string id);
    }
}