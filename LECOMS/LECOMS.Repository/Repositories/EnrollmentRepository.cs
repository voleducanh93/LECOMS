using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.Repository.Repositories;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LECOMS.Repository.Repositories
{
    public class EnrollmentRepository : Repository<Enrollment>, IEnrollmentRepository
    {
        public EnrollmentRepository(LecomDbContext db) : base(db) { }

        public async Task<Enrollment?> GetByUserAndCourseAsync(string userId, string courseId)
        {
            return await dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);
        }
    }
}