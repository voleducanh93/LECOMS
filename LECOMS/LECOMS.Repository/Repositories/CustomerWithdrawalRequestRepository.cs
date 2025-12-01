using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    /// <summary>
    /// Repository implementation cho CustomerWithdrawalRequest
    /// </summary>
    public class CustomerWithdrawalRequestRepository
        : Repository<CustomerWithdrawalRequest>, ICustomerWithdrawalRequestRepository
    {
        public CustomerWithdrawalRequestRepository(LecomDbContext db) : base(db) { }

        public async Task<IEnumerable<CustomerWithdrawalRequest>> GetByCustomerIdAsync(
            string customerId, int pageNumber, int pageSize)
        {
            return await dbSet
                .Include(w => w.Customer)
                .Where(w => w.CustomerId == customerId)
                .OrderByDescending(w => w.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomerWithdrawalRequest>> GetPendingAsync()
        {
            return await dbSet
                .Include(w => w.Customer)
                .Include(w => w.CustomerWallet)
                .Where(w => w.Status == WithdrawalStatus.Pending)
                .OrderBy(w => w.RequestedAt)
                .ToListAsync();
        }

        public async Task<CustomerWithdrawalRequest?> GetByIdWithDetailsAsync(string id)
        {
            return await dbSet
                .Include(w => w.Customer)
                .Include(w => w.CustomerWallet)
                .Include(w => w.ApprovedByUser)
                .FirstOrDefaultAsync(w => w.Id == id);
        }
    }
}