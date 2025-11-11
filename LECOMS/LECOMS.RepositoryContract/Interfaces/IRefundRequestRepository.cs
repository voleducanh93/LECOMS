using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    /// <summary>
    /// Repository cho RefundRequest
    /// </summary>
    public interface IRefundRequestRepository : IRepository<RefundRequest>
    {
        /// <summary>
        /// Lấy refund requests theo OrderId
        /// 1 order có thể có nhiều refund requests (partial refunds)
        /// </summary>
        Task<IEnumerable<RefundRequest>> GetByOrderIdAsync(string orderId);

        /// <summary>
        /// Lấy refund request với Order details (eager loading)
        /// </summary>
        Task<RefundRequest?> GetByIdWithDetailsAsync(string refundId);

        /// <summary>
        /// Lấy refund requests theo status
        /// Dùng cho admin dashboard
        /// </summary>
        Task<IEnumerable<RefundRequest>> GetByStatusAsync(
            RefundStatus status,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Lấy pending refund requests (chờ admin approve)
        /// </summary>
        Task<IEnumerable<RefundRequest>> GetPendingRequestsAsync();

        /// <summary>
        /// Lấy refund requests do user tạo
        /// </summary>
        Task<IEnumerable<RefundRequest>> GetByRequestedByAsync(
            string userId,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Lấy refund requests theo recipient
        /// </summary>
        Task<IEnumerable<RefundRequest>> GetByRecipientAsync(
            RefundRecipient recipient,
            RefundStatus? status = null,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Đếm số refund requests trong tháng của customer
        /// Dùng để check fraud (quá nhiều refund)
        /// </summary>
        Task<int> CountByCustomerInMonthAsync(string customerId, int year, int month);

        /// <summary>
        /// Tính tổng refund amount theo shop
        /// Dùng cho analytics
        /// </summary>
        Task<decimal> GetTotalRefundAmountByShopAsync(int shopId, DateTime fromDate, DateTime toDate);
    }
}