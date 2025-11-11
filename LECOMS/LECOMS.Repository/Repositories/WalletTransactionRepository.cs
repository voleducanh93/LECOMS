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
    /// Repository implementation cho WalletTransaction (ShopWallet)
    /// </summary>
    public class WalletTransactionRepository : Repository<WalletTransaction>, IWalletTransactionRepository
    {
        public WalletTransactionRepository(LecomDbContext db) : base(db) { }

        /// <summary>
        /// Lấy transactions theo ShopWalletId
        /// </summary>
        public async Task<IEnumerable<WalletTransaction>> GetByWalletIdAsync(
            string shopWalletId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Where(t => t.ShopWalletId == shopWalletId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy transactions theo type
        /// </summary>
        public async Task<IEnumerable<WalletTransaction>> GetByTypeAsync(
            string shopWalletId,
            WalletTransactionType type,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Where(t => t.ShopWalletId == shopWalletId && t.Type == type)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy transactions trong khoảng thời gian
        /// </summary>
        public async Task<IEnumerable<WalletTransaction>> GetByDateRangeAsync(
            string shopWalletId,
            DateTime fromDate,
            DateTime toDate)
        {
            return await dbSet
                .Where(t => t.ShopWalletId == shopWalletId
                    && t.CreatedAt >= fromDate
                    && t.CreatedAt <= toDate)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy transaction theo ReferenceId
        /// VD: Tìm transaction liên quan đến OrderId, RefundId
        /// </summary>
        public async Task<WalletTransaction?> GetByReferenceIdAsync(string referenceId)
        {
            return await dbSet
                .FirstOrDefaultAsync(t => t.ReferenceId == referenceId);
        }

        /// <summary>
        /// Tính tổng amount theo type trong khoảng thời gian
        /// </summary>
        public async Task<decimal> GetTotalAmountByTypeAsync(
            string shopWalletId,
            WalletTransactionType type,
            DateTime fromDate,
            DateTime toDate)
        {
            var total = await dbSet
                .Where(t => t.ShopWalletId == shopWalletId
                    && t.Type == type
                    && t.CreatedAt >= fromDate
                    && t.CreatedAt <= toDate)
                .SumAsync(t => t.Amount);

            return total;
        }
    }
}