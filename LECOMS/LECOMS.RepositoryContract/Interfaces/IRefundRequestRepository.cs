using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface IRefundRequestRepository : IRepository<RefundRequest>
    {
        Task<IEnumerable<RefundRequest>> GetByOrderIdAsync(string orderId);
        Task<RefundRequest?> GetByIdWithDetailsAsync(string refundId);
        Task<IEnumerable<RefundRequest>> GetByStatusAsync(RefundStatus status, int pageNumber = 1, int pageSize = 20);
        Task<IEnumerable<RefundRequest>> GetByRequestedByAsync(string userId, int pageNumber = 1, int pageSize = 20);
        Task<int> CountByCustomerInMonthAsync(string customerId, int year, int month);
        Task<decimal> GetTotalRefundAmountByShopAsync(int shopId, DateTime fromDate, DateTime toDate);
        Task<IEnumerable<RefundRequest>> GetByShopIdAsync(int shopId, RefundStatus? status = null);
    }
}