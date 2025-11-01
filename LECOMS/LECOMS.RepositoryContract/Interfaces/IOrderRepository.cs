using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetByIdWithDetailsAsync(string id);
        Task<IEnumerable<Order>> GetByUserIdAsync(string userId);
    }
}
