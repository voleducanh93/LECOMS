using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    /// <summary>
    /// Repository implementation cho CustomerWallet
    /// </summary>
    public class CustomerWalletRepository : Repository<CustomerWallet>, ICustomerWalletRepository
    {
        public CustomerWalletRepository(LecomDbContext db) : base(db) { }

        /// <summary>
        /// Lấy wallet theo CustomerId
        /// </summary>
        public async Task<CustomerWallet?> GetByCustomerIdAsync(string customerId, bool includeTransactions = false)
        {
            IQueryable<CustomerWallet> query = dbSet;

            if (includeTransactions)
            {
                query = query.Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt));
            }

            return await query.FirstOrDefaultAsync(w => w.CustomerId == customerId);
        }

        /// <summary>
        /// Lấy wallet với transactions (eager loading)
        /// </summary>
        public async Task<CustomerWallet?> GetByIdWithTransactionsAsync(string walletId)
        {
            return await dbSet
                .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt))
                .Include(w => w.Customer)
                .FirstOrDefaultAsync(w => w.Id == walletId);
        }

        /// <summary>
        /// Kiểm tra customer có đủ balance không
        /// </summary>
        public async Task<bool> HasSufficientBalanceAsync(string customerId, decimal amount)
        {
            var wallet = await GetByCustomerIdAsync(customerId);
            if (wallet == null) return false;

            return wallet.Balance >= amount;
        }

        /// <summary>
        /// Lấy danh sách customers có balance > 0
        /// </summary>
        public async Task<IEnumerable<CustomerWallet>> GetWalletsWithBalanceAsync(int pageNumber = 1, int pageSize = 20)
        {
            return await dbSet
                .Include(w => w.Customer)
                .Where(w => w.Balance > 0)
                .OrderByDescending(w => w.Balance)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy tổng balance của tất cả customers
        /// </summary>
        public async Task<decimal> GetTotalBalanceAsync()
        {
            var total = await dbSet.SumAsync(w => w.Balance);
            return total;
        }
    }
}