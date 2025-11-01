using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class LandingPageRepository : ILandingPageRepository
    {
        private readonly LecomDbContext _db;
        public LandingPageRepository(LecomDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Course>> GetAllCoursesAsync()
        {
            return await _db.Courses
                .Include(c => c.Category)
                .Include(c => c.Shop)
                .Include(c => c.Enrollments)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .ToListAsync();
        }

        public async Task<IEnumerable<CourseCategory>> GetAllCourseCategoriesAsync()
        {
            return await _db.CourseCategories
                .Include(c => c.Courses)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductCategory>> GetAllProductCategoriesAsync()
        {
            return await _db.ProductCategories
                .Include(c => c.Products)
                .ToListAsync();
        }
    }
}
