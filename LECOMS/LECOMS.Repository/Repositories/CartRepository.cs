using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class CartRepository : Repository<Cart>, ICartRepository
    {
        public CartRepository(LecomDbContext db) : base(db) { }

        public async Task<Cart?> GetByUserIdAsync(string userId, string? includeProperties = null)
        {
            IQueryable<Cart> query = dbSet;
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var include in includeProperties.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
                    query = query.Include(include.Trim());
            }
            return await query.FirstOrDefaultAsync(c => c.UserId == userId);
        }
    }
}