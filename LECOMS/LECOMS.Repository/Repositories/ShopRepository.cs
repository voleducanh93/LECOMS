using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class ShopRepository : Repository<Shop>, IShopRepository
    {
        private readonly LecomDbContext _db;

        public ShopRepository(LecomDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<Shop> GetBySellerIdAsync(string sellerId, string? includeProperties = null)
        {
            IQueryable<Shop> query = _db.Shops;

            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                foreach (var prop in includeProperties.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
                    query = query.Include(prop.Trim());
            }

            return await query.FirstOrDefaultAsync(s => s.SellerId == sellerId);
        }

        public async Task<bool> ExistsBySellerIdAsync(string sellerId)
        {
            return await _db.Shops.AnyAsync(s => s.SellerId == sellerId);
        }

        public async Task<Shop> UpdateShopAsync(Shop entity)
        {
            _db.Shops.Update(entity);
            return await Task.FromResult(entity);
        }
    }
}
