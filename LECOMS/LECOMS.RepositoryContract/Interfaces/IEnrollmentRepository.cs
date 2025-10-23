
using LECOMS.Data.Entities;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface IEnrollmentRepository : IRepository<Enrollment>
    {
        Task<Enrollment?> GetByUserAndCourseAsync(string userId, string courseId);
    }
}