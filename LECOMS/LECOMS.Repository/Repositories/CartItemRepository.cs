using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class CartItemRepository : Repository<CartItem>, ICartItemRepository
    {
        public CartItemRepository(LecomDbContext db) : base(db) { }

        public async Task<CartItem?> GetByCartAndProductAsync(string cartId, string productId)
        {
            return await dbSet.FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId);
        }
    }
}
