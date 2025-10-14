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
    public class LessonRepository : Repository<Lesson>, ILessonRepository
    {
        public LessonRepository(LecomDbContext db) : base(db) { }

        public async Task<IEnumerable<Lesson>> GetLessonsBySectionAsync(string sectionId)
        {
            return await dbSet.Where(l => l.CourseSectionId == sectionId).ToListAsync();
        }
    }
}
