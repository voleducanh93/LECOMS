using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface ITransactionOrderBreakdownRepository : IRepository<TransactionOrderBreakdown>
    {
        Task<TransactionOrderBreakdown?> GetByOrderIdAsync(string orderId);
        Task<IEnumerable<TransactionOrderBreakdown>> GetByTransactionIdAsync(string txId);
    }
}
