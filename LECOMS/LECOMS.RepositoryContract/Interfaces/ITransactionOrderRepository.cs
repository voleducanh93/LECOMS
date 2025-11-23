using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface ITransactionOrderRepository : IRepository<TransactionOrder>
    {
        Task<IEnumerable<TransactionOrder>> GetByTransactionIdAsync(string transactionId);
        Task<IEnumerable<TransactionOrder>> GetByOrderIdAsync(string orderId);
    }
}
