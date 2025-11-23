using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class TransactionOrderRepository : Repository<TransactionOrder>, ITransactionOrderRepository
    {
        private readonly LecomDbContext _db;

        public TransactionOrderRepository(LecomDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<TransactionOrder>> GetByTransactionIdAsync(string transactionId)
        {
            return await _db.TransactionOrders
                .Where(x => x.TransactionId == transactionId)
                .ToListAsync();
        }

        public async Task<IEnumerable<TransactionOrder>> GetByOrderIdAsync(string orderId)
        {
            return await _db.TransactionOrders
                .Where(x => x.OrderId == orderId)
                .ToListAsync();
        }
    }

}
