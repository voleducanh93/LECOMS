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
    public class LessonProductRepository : Repository<LessonProduct>, ILessonProductRepository
    {
        public LessonProductRepository(LecomDbContext db) : base(db) { }

        public async Task<bool> ExistsAsync(string lessonId, string productId)
            => await dbSet.AnyAsync(x => x.LessonId == lessonId && x.ProductId == productId);
    }
}
