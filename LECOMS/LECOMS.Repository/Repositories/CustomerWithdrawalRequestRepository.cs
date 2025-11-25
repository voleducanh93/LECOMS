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
    public class CustomerWithdrawalRequestRepository : Repository<CustomerWithdrawalRequest>, ICustomerWithdrawalRequestRepository
    {
        public CustomerWithdrawalRequestRepository(LecomDbContext db) : base(db) { }

        /// <summary>
        /// Lấy Yêu cầu rút tiền theo CustomerId
        /// </summary>
        public async Task<IEnumerable<CustomerWithdrawalRequest>> GetByCustomerIdAsync(
            string customerId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Include(w => w.Customer)
                .Include(w => w.ApprovedByUser)
                .Where(w => w.CustomerId == customerId)
                .OrderByDescending(w => w.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy Yêu cầu rút tiền với Customer details
        /// </summary>
        public async Task<CustomerWithdrawalRequest?> GetByIdWithDetailsAsync(string withdrawalId)
        {
            return await dbSet
                .Include(w => w.Customer)
                .Include(w => w.CustomerWallet)
                .Include(w => w.ApprovedByUser)
                .FirstOrDefaultAsync(w => w.Id == withdrawalId);
        }

        /// <summary>
        /// Lấy Yêu cầu rút tiền theo status
        /// </summary>
        public async Task<IEnumerable<CustomerWithdrawalRequest>> GetByStatusAsync(
            WithdrawalStatus status,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Include(w => w.Customer)
                .Include(w => w.ApprovedByUser)
                .Where(w => w.Status == status)
                .OrderByDescending(w => w.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy pending Yêu cầu rút tiền
        /// </summary>
        public async Task<IEnumerable<CustomerWithdrawalRequest>> GetPendingRequestsAsync()
        {
            return await dbSet
                .Include(w => w.Customer)
                .Include(w => w.CustomerWallet)
                .Where(w => w.Status == WithdrawalStatus.Pending)
                .OrderBy(w => w.RequestedAt) // Oldest first
                .ToListAsync();
        }

        /// <summary>
        /// Lấy approved withdrawals (chờ processing)
        /// </summary>
        public async Task<IEnumerable<CustomerWithdrawalRequest>> GetApprovedRequestsAsync()
        {
            return await dbSet
                .Include(w => w.Customer)
                .Include(w => w.CustomerWallet)
                .Where(w => w.Status == WithdrawalStatus.Approved)
                .OrderBy(w => w.ApprovedAt)
                .ToListAsync();
        }
    }
}