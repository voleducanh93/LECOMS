using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    /// <summary>
    /// Repository implementation cho RefundRequest
    /// </summary>
    public class RefundRequestRepository : Repository<RefundRequest>, IRefundRequestRepository
    {
        public RefundRequestRepository(LecomDbContext db) : base(db) { }

        /// <summary>
        /// Lấy refund requests theo OrderId
        /// </summary>
        public async Task<IEnumerable<RefundRequest>> GetByOrderIdAsync(string orderId)
        {
            return await dbSet
                .Include(r => r.Order)
                .Include(r => r.RequestedByUser)
                .Include(r => r.ProcessedByUser)
                .Where(r => r.OrderId == orderId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy refund request với Order details (eager loading)
        /// </summary>
        public async Task<RefundRequest?> GetByIdWithDetailsAsync(string refundId)
        {
            return await dbSet
                .Include(r => r.Order)
                    .ThenInclude(o => o.Shop)
                .Include(r => r.Order)
                    .ThenInclude(o => o.User)
                .Include(r => r.RequestedByUser)
                .Include(r => r.ProcessedByUser)
                .FirstOrDefaultAsync(r => r.Id == refundId);
        }

        /// <summary>
        /// Lấy refund requests theo status
        /// </summary>
        public async Task<IEnumerable<RefundRequest>> GetByStatusAsync(
            RefundStatus status,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Include(r => r.Order)
                .Include(r => r.RequestedByUser)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy pending refund requests (chờ admin approve)
        /// </summary>
        public async Task<IEnumerable<RefundRequest>> GetPendingRequestsAsync()
        {
            return await dbSet
                .Include(r => r.Order)
                    .ThenInclude(o => o.Shop)
                .Include(r => r.RequestedByUser)
                .Where(r => r.Status == RefundStatus.Pending)
                .OrderBy(r => r.RequestedAt) // Oldest first
                .ToListAsync();
        }

        /// <summary>
        /// Lấy refund requests do user tạo
        /// </summary>
        public async Task<IEnumerable<RefundRequest>> GetByRequestedByAsync(
            string userId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Include(r => r.Order)
                .Include(r => r.ProcessedByUser)
                .Where(r => r.RequestedBy == userId)
                .OrderByDescending(r => r.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy refund requests theo recipient
        /// </summary>
        public async Task<IEnumerable<RefundRequest>> GetByRecipientAsync(
            RefundRecipient recipient,
            RefundStatus? status = null,
            int pageNumber = 1,
            int pageSize = 20)
        {
            IQueryable<RefundRequest> query = dbSet
                .Include(r => r.Order)
                .Include(r => r.RequestedByUser)
                .Where(r => r.Recipient == recipient);

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            return await query
                .OrderByDescending(r => r.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Đếm số refund requests trong tháng của customer
        /// Dùng để check fraud
        /// </summary>
        public async Task<int> CountByCustomerInMonthAsync(string customerId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            return await dbSet
                .Where(r => r.Order.UserId == customerId
                    && r.Status == RefundStatus.Completed
                    && r.RequestedAt >= startDate
                    && r.RequestedAt < endDate)
                .CountAsync();
        }

        /// <summary>
        /// Tính tổng refund amount theo shop
        /// </summary>
        public async Task<decimal> GetTotalRefundAmountByShopAsync(int shopId, DateTime fromDate, DateTime toDate)
        {
            var total = await dbSet
                .Where(r => r.Order.ShopId == shopId
                    && r.Status == RefundStatus.Completed
                    && r.Recipient == RefundRecipient.Customer // Shop bị trừ tiền
                    && r.RequestedAt >= fromDate
                    && r.RequestedAt <= toDate)
                .SumAsync(r => r.RefundAmount);

            return total;
        }
    }
}