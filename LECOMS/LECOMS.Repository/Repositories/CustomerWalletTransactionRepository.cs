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
    /// Repository implementation cho CustomerWalletTransaction
    /// </summary>
    public class CustomerWalletTransactionRepository : Repository<CustomerWalletTransaction>, ICustomerWalletTransactionRepository
    {
        public CustomerWalletTransactionRepository(LecomDbContext db) : base(db) { }

        /// <summary>
        /// Lấy transactions theo CustomerWalletId
        /// </summary>
        public async Task<IEnumerable<CustomerWalletTransaction>> GetByWalletIdAsync(
            string customerWalletId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Where(t => t.CustomerWalletId == customerWalletId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy transactions theo type
        /// </summary>
        public async Task<IEnumerable<CustomerWalletTransaction>> GetByTypeAsync(
            string customerWalletId,
            WalletTransactionType type,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Where(t => t.CustomerWalletId == customerWalletId && t.Type == type)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy transactions trong khoảng thời gian
        /// </summary>
        public async Task<IEnumerable<CustomerWalletTransaction>> GetByDateRangeAsync(
            string customerWalletId,
            DateTime fromDate,
            DateTime toDate)
        {
            return await dbSet
                .Where(t => t.CustomerWalletId == customerWalletId
                    && t.CreatedAt >= fromDate
                    && t.CreatedAt <= toDate)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy transaction theo ReferenceId
        /// </summary>
        public async Task<CustomerWalletTransaction?> GetByReferenceIdAsync(string referenceId)
        {
            return await dbSet
                .FirstOrDefaultAsync(t => t.ReferenceId == referenceId);
        }
    }
}