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
    public class CourseCategoryRepository : Repository<CourseCategory>, ICourseCategoryRepository
    {
        private readonly LecomDbContext _db;
        public CourseCategoryRepository(LecomDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<CourseCategory?> GetByNameAsync(string name)
        {
            return await _db.CourseCategories.FirstOrDefaultAsync(c => c.Name == name);
        }
    }
}
