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
    public class RefundRequestRepository : Repository<RefundRequest>, IRefundRequestRepository
    {
        public RefundRequestRepository(LecomDbContext db) : base(db) { }

        public async Task<IEnumerable<RefundRequest>> GetByOrderIdAsync(string orderId)
        {
            return await dbSet
                .Include(r => r.Order)
                .Include(r => r.RequestedByUser)
                .Include(r => r.ShopResponseByUser)
                .Where(r => r.OrderId == orderId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<RefundRequest?> GetByIdWithDetailsAsync(string refundId)
        {
            return await dbSet
                .Include(r => r.Order)
                    .ThenInclude(o => o.Shop)
                .Include(r => r.Order)
                    .ThenInclude(o => o.User)
                .Include(r => r.RequestedByUser)
                .Include(r => r.ShopResponseByUser)
                .FirstOrDefaultAsync(r => r.Id == refundId);
        }

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

        public async Task<IEnumerable<RefundRequest>> GetByRequestedByAsync(
            string userId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Include(r => r.Order)
                .Include(r => r.ShopResponseByUser)
                .Where(r => r.RequestedBy == userId)
                .OrderByDescending(r => r.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountByCustomerInMonthAsync(string customerId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            return await dbSet
                .Where(r => r.Order.UserId == customerId
                    && r.Status == RefundStatus.Refunded
                    && r.RequestedAt >= startDate
                    && r.RequestedAt < endDate)
                .CountAsync();
        }

        public async Task<decimal> GetTotalRefundAmountByShopAsync(int shopId, DateTime fromDate, DateTime toDate)
        {
            return await dbSet
                .Where(r => r.Order.ShopId == shopId
                    && r.Status == RefundStatus.Refunded
                    && r.RequestedAt >= fromDate
                    && r.RequestedAt <= toDate)
                .SumAsync(r => r.RefundAmount);
        }

        public async Task<IEnumerable<RefundRequest>> GetByShopIdAsync(int shopId, RefundStatus? status = null)
        {
            var query = dbSet
                .Include(r => r.Order)
                .Include(r => r.RequestedByUser)
                .Where(r => r.Order.ShopId == shopId);

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            return await query
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }
    }
}