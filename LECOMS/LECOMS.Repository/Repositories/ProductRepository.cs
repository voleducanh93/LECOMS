using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly LecomDbContext _db;
        public ProductRepository(LecomDbContext db) : base(db) => _db = db;

        public async Task<IEnumerable<Product>> GetAllByShopAsync(int shopId, string? includeProperties = null)
        {
            IQueryable<Product> query = _db.Products;

            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                foreach (var prop in includeProperties.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
                    query = query.Include(prop.Trim());
            }

            return await query
                .Where(p => p.Active == 1)
                .ToListAsync();
        }

        public async Task<bool> ExistsSlugAsync(string slug)
        {
            return await _db.Products.AnyAsync(p => p.Slug == slug);
        }
    }
}