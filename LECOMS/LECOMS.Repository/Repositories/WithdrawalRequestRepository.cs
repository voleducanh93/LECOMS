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
    public class WithdrawalRequestRepository
        : Repository<WithdrawalRequest>, IWithdrawalRequestRepository
    {
        public WithdrawalRequestRepository(LecomDbContext db) : base(db) { }

        public async Task<IEnumerable<WithdrawalRequest>> GetByShopIdAsync(
            int shopId, int pageNumber, int pageSize)
        {
            return await dbSet
                .Include(w => w.Shop)
                .Where(w => w.ShopId == shopId)
                .OrderByDescending(w => w.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<WithdrawalRequest>> GetPendingAsync()
        {
            return await dbSet
                .Include(w => w.Shop)
                    .ThenInclude(s => s.Seller)
                .Include(w => w.ShopWallet)
                .Where(w => w.Status == WithdrawalStatus.Pending)
                .OrderBy(w => w.RequestedAt)
                .ToListAsync();
        }

        public async Task<WithdrawalRequest?> GetByIdWithDetailsAsync(string id)
        {
            return await dbSet
                .Include(w => w.Shop)
                    .ThenInclude(s => s.Seller)
                .Include(w => w.ShopWallet)
                .Include(w => w.ApprovedByUser)
                .FirstOrDefaultAsync(w => w.Id == id);
        }
    }
}