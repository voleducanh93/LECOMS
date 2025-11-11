using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    /// <summary>
    /// Repository implementation cho Cart
    /// </summary>
    public class CartRepository : Repository<Cart>, ICartRepository
    {
        public CartRepository(LecomDbContext db) : base(db) { }

        /// <summary>
        /// Lấy cart theo UserId
        /// </summary>
        public async Task<Cart?> GetByUserIdAsync(string userId, string? includeProperties = null)
        {
            IQueryable<Cart> query = dbSet;

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var include in includeProperties.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(include.Trim());
                }
            }

            return await query.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        /// <summary>
        /// Đếm số items trong cart ⭐ NEW
        /// </summary>
        public async Task<int> GetCartItemCountAsync(string userId)
        {
            var cart = await dbSet
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return cart?.Items.Count ?? 0;
        }

        /// <summary>
        /// Tính tổng giá trị cart ⭐ NEW
        /// </summary>
        public async Task<decimal> GetCartTotalAsync(string userId)
        {
            var cart = await dbSet
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return 0;

            return cart.Items.Sum(i => i.Product.Price * i.Quantity);
        }

        /// <summary>
        /// Kiểm tra cart có rỗng không ⭐ NEW
        /// </summary>
        public async Task<bool> IsCartEmptyAsync(string userId)
        {
            var cart = await dbSet
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return cart == null || !cart.Items.Any();
        }
    }
}