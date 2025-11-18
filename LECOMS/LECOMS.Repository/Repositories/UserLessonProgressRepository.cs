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
    public class UserLessonProgressRepository
        : Repository<UserLessonProgress>, IUserLessonProgressRepository
    {
        public UserLessonProgressRepository(LecomDbContext db) : base(db) { }

        public Task<UserLessonProgress?> GetProgressAsync(string userId, string lessonId)
        {
            return dbSet.FirstOrDefaultAsync(
                x => x.UserId == userId && x.LessonId == lessonId
            );
        }
    }
}
