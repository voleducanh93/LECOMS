using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    /// <summary>
    /// Repository implementation cho ShopWallet
    /// </summary>
    public class ShopWalletRepository : Repository<ShopWallet>, IShopWalletRepository
    {
        public ShopWalletRepository(LecomDbContext db) : base(db) { }

        /// <summary>
        /// Lấy wallet theo ShopId
        /// Relationship 1-1
        /// </summary>
        public async Task<ShopWallet?> GetByShopIdAsync(int shopId, bool includeTransactions = false)
        {
            IQueryable<ShopWallet> query = dbSet;

            if (includeTransactions)
            {
                query = query.Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt));
            }

            return await query.FirstOrDefaultAsync(w => w.ShopId == shopId);
        }

        /// <summary>
        /// Lấy wallet với transactions (eager loading)
        /// </summary>
        public async Task<ShopWallet?> GetByIdWithTransactionsAsync(string walletId)
        {
            return await dbSet
                .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt))
                .Include(w => w.Shop)
                .FirstOrDefaultAsync(w => w.Id == walletId);
        }

        /// <summary>
        /// Lấy wallet với withdrawal requests
        /// </summary>
        public async Task<ShopWallet?> GetByIdWithWithdrawalsAsync(string walletId)
        {
            return await dbSet
                .Include(w => w.WithdrawalRequests.OrderByDescending(wr => wr.RequestedAt))
                .Include(w => w.Shop)
                .FirstOrDefaultAsync(w => w.Id == walletId);
        }

        /// <summary>
        /// Kiểm tra shop có đủ balance để rút tiền không
        /// </summary>
        public async Task<bool> HasSufficientBalanceAsync(int shopId, decimal amount)
        {
            var wallet = await GetByShopIdAsync(shopId);
            if (wallet == null) return false;

            return wallet.AvailableBalance >= amount;
        }

        /// <summary>
        /// Lấy tổng available balance của tất cả shops
        /// Dùng cho admin report
        /// </summary>
        public async Task<decimal> GetTotalAvailableBalanceAsync()
        {
            var total = await dbSet.SumAsync(w => w.AvailableBalance);
            return total;
        }

        /// <summary>
        /// Lấy tổng pending balance của tất cả shops
        /// </summary>
        public async Task<decimal> GetTotalPendingBalanceAsync()
        {
            var total = await dbSet.SumAsync(w => w.PendingBalance);
            return total;
        }
    }
}