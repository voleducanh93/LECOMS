using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    /// <summary>
    /// Repository cho Transaction (PayOS payments)
    /// </summary>
    public interface ITransactionRepository : IRepository<Transaction>
    {
        /// <summary>
        /// Lấy transaction theo OrderId
        /// Relationship 1-1: 1 order có 1 transaction duy nhất
        /// </summary>
        Task<Transaction?> GetByOrderIdAsync(string orderId);

        /// <summary>
        /// Lấy transaction theo PayOS Transaction ID
        /// Dùng khi nhận webhook từ PayOS
        /// </summary>
        Task<Transaction?> GetByPayOSTransactionIdAsync(string payOSTransactionId);

        /// <summary>
        /// Lấy danh sách transactions theo shop
        /// Dùng cho report, analytics
        /// </summary>
        Task<IEnumerable<Transaction>> GetByShopIdAsync(int shopId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Lấy danh sách transactions trong khoảng thời gian
        /// Dùng cho reconciliation, reporting
        /// </summary>
        Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, string? includeProperties = null);

        /// <summary>
        /// Tính tổng platform fee trong khoảng thời gian
        /// Dùng cho revenue report
        /// </summary>
        Task<decimal> GetTotalPlatformFeeAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Lấy transactions pending (chưa complete)
        /// Dùng cho monitoring
        /// </summary>
        Task<IEnumerable<Transaction>> GetPendingTransactionsAsync();
    }
}