using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface ITransactionOrderService
    {
        Task<IEnumerable<TransactionOrder>> GetByTransactionIdAsync(string transactionId);
        Task<IEnumerable<TransactionOrder>> GetByOrderIdAsync(string orderId);
        Task CreateMappingAsync(string transactionId, IEnumerable<string> orderIds);
    }
}
