using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    /// <summary>
    /// Repository implementation cho Order - UPDATED for Marketplace Payment
    /// </summary>
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(LecomDbContext db) : base(db) { }

        /// <summary>
        /// Lấy order with full details
        /// </summary>
        public async Task<Order?> GetByIdWithDetailsAsync(string id)
        {
            return await dbSet
                .Include(o => o.User)
                .Include(o => o.Shop)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.Images)
                .Include(o => o.Payments)
                .Include(o => o.Shipments)
                .Include(o => o.RefundRequests)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        /// <summary>
        /// Lấy orders theo UserId
        /// </summary>
        public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)
        {
            return await dbSet
                .Where(o => o.UserId == userId)
                .Include(o => o.Shop)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.Images)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy orders theo ShopId
        /// </summary>
        public async Task<IEnumerable<Order>> GetByShopIdAsync(int shopId, int pageNumber = 1, int pageSize = 20)
        {
            return await dbSet
                .Where(o => o.ShopId == shopId)
                .Include(o => o.User)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy order theo OrderCode
        /// </summary>
        public async Task<Order?> GetByOrderCodeAsync(string orderCode)
        {
            return await dbSet
                .Include(o => o.User)
                .Include(o => o.Shop)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        }

        /// <summary>
        /// Lấy orders cần release balance
        /// Orders đã completed + paid + chưa release + quá holding period
        /// </summary>
        public async Task<IEnumerable<Order>> GetOrdersToReleaseBalanceAsync(DateTime cutoffDate)
        {
            return await dbSet
                .Include(o => o.Shop)
                .Where(o => o.PaymentStatus == PaymentStatus.Paid
                    && o.Status == OrderStatus.Completed
                    && o.CompletedAt.HasValue
                    && o.CompletedAt.Value <= cutoffDate
                    && !o.BalanceReleased)
                .OrderBy(o => o.CompletedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy orders theo PaymentStatus
        /// </summary>
        public async Task<IEnumerable<Order>> GetByPaymentStatusAsync(
            PaymentStatus paymentStatus,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Where(o => o.PaymentStatus == paymentStatus)
                .Include(o => o.User)
                .Include(o => o.Shop)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy orders theo ShopId và Status
        /// </summary>
        public async Task<IEnumerable<Order>> GetByShopAndStatusAsync(
            int shopId,
            OrderStatus status,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Where(o => o.ShopId == shopId && o.Status == status)
                .Include(o => o.User)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Đếm số orders theo điều kiện
        /// </summary>
        public async Task<int> CountOrdersAsync(Expression<Func<Order, bool>> predicate)
        {
            return await dbSet.CountAsync(predicate);
        }

        /// <summary>
        /// Tính tổng doanh thu theo shop trong khoảng thời gian
        /// </summary>
        public async Task<decimal> GetTotalRevenueByShopAsync(int shopId, DateTime fromDate, DateTime toDate)
        {
            return await dbSet
                .Where(o => o.ShopId == shopId
                    && o.PaymentStatus == PaymentStatus.Paid
                    && o.CreatedAt >= fromDate
                    && o.CreatedAt <= toDate)
                .SumAsync(o => o.Total);
        }

        /// <summary>
        /// Lấy orders theo Status
        /// </summary>
        public async Task<IEnumerable<Order>> GetByStatusAsync(
            OrderStatus status,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Where(o => o.Status == status)
                .Include(o => o.User)
                .Include(o => o.Shop)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy orders theo date range
        /// </summary>
        public async Task<IEnumerable<Order>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            int? shopId = null)
        {
            var query = dbSet
                .Include(o => o.Shop)
                .Include(o => o.User)
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate);

            if (shopId.HasValue)
            {
                query = query.Where(o => o.ShopId == shopId.Value);
            }

            return await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
    }
}