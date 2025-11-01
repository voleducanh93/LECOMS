using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(LecomDbContext db) : base(db) { }

        public async Task<Order?> GetByIdWithDetailsAsync(string id)
        {
            return await dbSet
                .Include(o => o.Details)
                    .ThenInclude(d => d.Product)
                .Include(o => o.Payments)
                .Include(o => o.Shipments)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)
        {
            return await dbSet
                .Where(o => o.UserId == userId)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
    }
}