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
    public class CourseSectionRepository : Repository<CourseSection>, ICourseSectionRepository
    {
        public CourseSectionRepository(LecomDbContext db) : base(db) { }

        public async Task<IEnumerable<CourseSection>> GetSectionsByCourseAsync(string courseId)
        {
            return await dbSet.Where(s => s.CourseId == courseId).ToListAsync();
        }
    }
}
