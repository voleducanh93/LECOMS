using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface ICartRepository : IRepository<Cart>
    {
        Task<Cart?> GetByUserIdAsync(string userId, string? includeProperties = null);
    }
}
