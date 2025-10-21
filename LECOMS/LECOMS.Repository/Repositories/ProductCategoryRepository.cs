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
    public class ProductCategoryRepository : Repository<ProductCategory>, IProductCategoryRepository
    {
        private readonly LecomDbContext _db;
        public ProductCategoryRepository(LecomDbContext db) : base(db) => _db = db;

        public async Task<ProductCategory?> GetByNameAsync(string name)
        {
            return await _db.ProductCategories.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> ExistsSlugAsync(string slug)
        {
            return await _db.ProductCategories.AnyAsync(c => c.Slug == slug);
        }
    }
}
