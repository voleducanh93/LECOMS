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
    /// Repository implementation cho WithdrawalRequest (Shop)
    /// </summary>
    public class WithdrawalRequestRepository : Repository<WithdrawalRequest>, IWithdrawalRequestRepository
    {
        public WithdrawalRequestRepository(LecomDbContext db) : base(db) { }

        /// <summary>
        /// Lấy Yêu cầu rút tiền theo ShopId
        /// </summary>
        public async Task<IEnumerable<WithdrawalRequest>> GetByShopIdAsync(
            int shopId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Include(w => w.Shop)
                .Include(w => w.ApprovedByUser)
                .Where(w => w.ShopId == shopId)
                .OrderByDescending(w => w.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy Yêu cầu rút tiền với Shop details (eager loading)
        /// </summary>
        public async Task<WithdrawalRequest?> GetByIdWithDetailsAsync(string withdrawalId)
        {
            return await dbSet
                .Include(w => w.Shop)
                    .ThenInclude(s => s.Seller)
                .Include(w => w.ShopWallet)
                .Include(w => w.ApprovedByUser)
                .FirstOrDefaultAsync(w => w.Id == withdrawalId);
        }

        /// <summary>
        /// Lấy Yêu cầu rút tiền theo status
        /// </summary>
        public async Task<IEnumerable<WithdrawalRequest>> GetByStatusAsync(
            WithdrawalStatus status,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Include(w => w.Shop)
                .Include(w => w.ApprovedByUser)
                .Where(w => w.Status == status)
                .OrderByDescending(w => w.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy pending Yêu cầu rút tiền (chờ admin approve)
        /// Dùng cho admin dashboard
        /// </summary>
        public async Task<IEnumerable<WithdrawalRequest>> GetPendingRequestsAsync()
        {
            return await dbSet
                .Include(w => w.Shop)
                    .ThenInclude(s => s.Seller)
                .Include(w => w.ShopWallet)
                .Where(w => w.Status == WithdrawalStatus.Pending)
                .OrderBy(w => w.RequestedAt) // Oldest first (FIFO)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy approved withdrawals (chờ processing)
        /// Dùng cho background job xử lý chuyển khoản
        /// </summary>
        public async Task<IEnumerable<WithdrawalRequest>> GetApprovedRequestsAsync()
        {
            return await dbSet
                .Include(w => w.Shop)
                    .ThenInclude(s => s.Seller)
                .Include(w => w.ShopWallet)
                .Where(w => w.Status == WithdrawalStatus.Approved)
                .OrderBy(w => w.ApprovedAt) // Oldest approved first
                .ToListAsync();
        }

        /// <summary>
        /// Đếm số Yêu cầu rút tiền trong tháng của shop
        /// Dùng để giới hạn số lần rút tiền
        /// </summary>
        public async Task<int> CountByShopInMonthAsync(int shopId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            return await dbSet
                .Where(w => w.ShopId == shopId
                    && w.Status == WithdrawalStatus.Completed
                    && w.RequestedAt >= startDate
                    && w.RequestedAt < endDate)
                .CountAsync();
        }

        /// <summary>
        /// Tính tổng Số tiền rút của shop
        /// Dùng cho reporting
        /// </summary>
        public async Task<decimal> GetTotalWithdrawalAmountByShopAsync(int shopId, DateTime fromDate, DateTime toDate)
        {
            var total = await dbSet
                .Where(w => w.ShopId == shopId
                    && w.Status == WithdrawalStatus.Completed
                    && w.RequestedAt >= fromDate
                    && w.RequestedAt <= toDate)
                .SumAsync(w => w.Amount);

            return total;
        }
    }
}