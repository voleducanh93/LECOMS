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
    public class ProductImageRepository : Repository<ProductImage>, IProductImageRepository
    {
        private readonly LecomDbContext _db;
        public ProductImageRepository(LecomDbContext db) : base(db) { _db = db; }

        public async Task<IEnumerable<ProductImage>> GetByProductIdAsync(string productId)
            => await _db.ProductImages.Where(x => x.ProductId == productId)
                                      .OrderBy(x => x.OrderIndex).ToListAsync();

        public async Task DeleteAllByProductIdAsync(string productId)
        {
            var imgs = _db.ProductImages.Where(x => x.ProductId == productId);
            _db.ProductImages.RemoveRange(imgs);
        }
    }
}
