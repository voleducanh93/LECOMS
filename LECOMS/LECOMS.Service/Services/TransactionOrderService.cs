using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class TransactionOrderService : ITransactionOrderService
    {
        private readonly IUnitOfWork _uow;

        public TransactionOrderService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IEnumerable<TransactionOrder>> GetByTransactionIdAsync(string transactionId)
        {
            return await _uow.TransactionOrders.GetByTransactionIdAsync(transactionId);
        }

        public async Task<IEnumerable<TransactionOrder>> GetByOrderIdAsync(string orderId)
        {
            return await _uow.TransactionOrders.GetByOrderIdAsync(orderId);
        }

        public async Task CreateMappingAsync(string transactionId, IEnumerable<string> orderIds)
        {
            foreach (var oid in orderIds)
            {
                await _uow.TransactionOrders.AddAsync(new TransactionOrder
                {
                    TransactionId = transactionId,
                    OrderId = oid
                });
            }

            await _uow.CompleteAsync();
        }
    }
}
