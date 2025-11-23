using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class TransactionOrderBreakdownRepository :
        Repository<TransactionOrderBreakdown>, ITransactionOrderBreakdownRepository
    {
        private readonly LecomDbContext _db;

        public TransactionOrderBreakdownRepository(LecomDbContext db) : base(db)
        {
            _db = db;
        }

        // ============================================
        // Lấy breakdown theo OrderId
        // ============================================
        public async Task<TransactionOrderBreakdown?> GetByOrderIdAsync(string orderId)
        {
            return await _db.TransactionOrderBreakdowns
                .Include(b => b.TransactionOrder)
                .ThenInclude(to => to.Order)
                .FirstOrDefaultAsync(b => b.TransactionOrder.OrderId == orderId);
        }

        // ============================================
        // Lấy breakdown theo TransactionId
        // ============================================
        public async Task<IEnumerable<TransactionOrderBreakdown>> GetByTransactionIdAsync(string txId)
        {
            return await _db.TransactionOrderBreakdowns
                .Include(b => b.TransactionOrder)
                .ThenInclude(to => to.Transaction)
                .Where(b => b.TransactionOrder.TransactionId == txId)
                .ToListAsync();
        }
    }
}
