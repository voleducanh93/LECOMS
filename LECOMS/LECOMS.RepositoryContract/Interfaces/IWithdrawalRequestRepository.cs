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
        /// <summary>
        /// Lấy withdrawal requests theo ShopId
        /// </summary>
        Task<IEnumerable<WithdrawalRequest>> GetByShopIdAsync(
            int shopId,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Lấy withdrawal request với Shop details (eager loading)
        /// </summary>
        Task<WithdrawalRequest?> GetByIdWithDetailsAsync(string withdrawalId);

        /// <summary>
        /// Lấy withdrawal requests theo status
        /// </summary>
        Task<IEnumerable<WithdrawalRequest>> GetByStatusAsync(
            WithdrawalStatus status,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Lấy pending withdrawal requests (chờ admin approve)
        /// </summary>
        Task<IEnumerable<WithdrawalRequest>> GetPendingRequestsAsync();

        /// <summary>
        /// Lấy approved withdrawals (chờ processing)
        /// Dùng cho background job
        /// </summary>
        Task<IEnumerable<WithdrawalRequest>> GetApprovedRequestsAsync();

        /// <summary>
        /// Đếm số withdrawal requests trong tháng của shop
        /// </summary>
        Task<int> CountByShopInMonthAsync(int shopId, int year, int month);

        /// <summary>
        /// Tính tổng withdrawal amount của shop
        /// </summary>
        Task<decimal> GetTotalWithdrawalAmountByShopAsync(int shopId, DateTime fromDate, DateTime toDate);
    }
}