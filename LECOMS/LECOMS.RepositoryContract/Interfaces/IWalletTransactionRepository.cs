using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    /// <summary>
    /// Repository cho WalletTransaction (ShopWallet transactions)
    /// </summary>
    public interface IWalletTransactionRepository : IRepository<WalletTransaction>
    {
        /// <summary>
        /// Lấy transactions theo ShopWalletId
        /// </summary>
        Task<IEnumerable<WalletTransaction>> GetByWalletIdAsync(
            string shopWalletId,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Lấy transactions theo type
        /// </summary>
        Task<IEnumerable<WalletTransaction>> GetByTypeAsync(
            string shopWalletId,
            WalletTransactionType type,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Lấy transactions trong khoảng thời gian
        /// </summary>
        Task<IEnumerable<WalletTransaction>> GetByDateRangeAsync(
            string shopWalletId,
            DateTime fromDate,
            DateTime toDate);

        /// <summary>
        /// Lấy transaction theo ReferenceId
        /// VD: Tìm transaction liên quan đến OrderId, RefundId
        /// </summary>
        Task<WalletTransaction?> GetByReferenceIdAsync(string referenceId);

        /// <summary>
        /// Tính tổng amount theo type trong khoảng thời gian
        /// Dùng cho reporting
        /// </summary>
        Task<decimal> GetTotalAmountByTypeAsync(
            string shopWalletId,
            WalletTransactionType type,
            DateTime fromDate,
            DateTime toDate);
    }
}