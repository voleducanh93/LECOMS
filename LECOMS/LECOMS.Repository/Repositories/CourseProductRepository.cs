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
    public class CourseProductRepository : Repository<CourseProduct>, ICourseProductRepository
    {
        public CourseProductRepository(LecomDbContext db) : base(db) { }

        public async Task<bool> ExistsAsync(string courseId, string productId)
        {
            return await dbSet.AnyAsync(cp => cp.CourseId == courseId && cp.ProductId == productId);
        }
    }
}
