using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly LecomDbContext _db;

        public ProductRepository(LecomDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Product>> GetAllByShopAsync(int shopId, string? includeProperties = null)
        {
            IQueryable<Product> query = _db.Products
                .Where(p => p.Active == 1 && p.ShopId == shopId)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .OrderByDescending(p => p.LastUpdatedAt);

            return await query.ToListAsync();
        }

        public async Task<bool> ExistsSlugAsync(string slug)
        {
            return await _db.Products.AnyAsync(p => p.Slug == slug);
        }
    }
}
