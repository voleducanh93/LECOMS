using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    /// <summary>
    /// Repository implementation cho CartItem
    /// </summary>
    public class CartItemRepository : Repository<CartItem>, ICartItemRepository
    {
        private readonly LecomDbContext _db;
        public CartItemRepository(LecomDbContext db) : base(db)
        {
            _db = db;
        }

        /// <summary>
        /// Lấy cart item theo CartId và ProductId
        /// </summary>
        public async Task<CartItem?> GetByCartAndProductAsync(string cartId, string productId)
        {
            return await dbSet
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId);
        }

        /// <summary>
        /// Lấy tất cả items của cart
        /// </summary>
        public async Task<IEnumerable<CartItem>> GetByCartIdAsync(string cartId)
        {
            return await dbSet
                .Include(ci => ci.Product)
                    .ThenInclude(p => p.Images)
                .Where(ci => ci.CartId == cartId)
                .ToListAsync();
        }

        /// <summary>
        /// Xóa tất cả items của cart
        /// </summary>
        public async Task DeleteAllByCartIdAsync(string cartId)
        {
            var items = await dbSet.Where(ci => ci.CartId == cartId).ToListAsync();

            if (items.Any())
            {
                dbSet.RemoveRange(items);
                await _db.SaveChangesAsync();
            }
        }
    }
}