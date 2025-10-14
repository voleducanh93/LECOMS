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
    public class CourseRepository : Repository<Course>, ICourseRepository
    {
        public CourseRepository(LecomDbContext db) : base(db) { }

        public async Task<IEnumerable<Course>> GetCoursesByShopAsync(int shopId)
        {
            return await dbSet.Where(c => c.ShopId == shopId).ToListAsync();
        }
    }
}
