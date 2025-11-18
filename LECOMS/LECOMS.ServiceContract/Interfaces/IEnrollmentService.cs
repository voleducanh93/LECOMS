using System.Threading.Tasks;
using LECOMS.Data.DTOs.Course;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IEnrollmentService
    {
        Task<EnrollmentDTO> EnrollAsync(string userId, string courseId);
        Task<EnrollmentDTO?> GetEnrollmentAsync(string userId, string courseId);
        Task<IEnumerable<EnrollmentDTO>> GetUserEnrollmentsAsync(string userId);
        Task<object> GetLearningDetailAsync(string userId, string courseId);
        Task<bool> CompleteLessonAsync(string userId, string lessonId);

    }
}