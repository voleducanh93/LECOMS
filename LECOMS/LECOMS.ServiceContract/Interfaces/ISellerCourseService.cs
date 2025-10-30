using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.Entities;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface ISellerCourseService
    {
        Task<Course> CreateCourseAsync(CreateCourseDto dto);

        Task<CourseSection> CreateSectionAsync(CreateSectionDto dto);

        Task<Lesson> CreateLessonAsync(CreateLessonDto dto);

        Task<LessonProduct> LinkLessonProductAsync(LinkLessonProductDto dto);

        Task<bool> UnlinkLessonProductAsync(string lessonId, string productId);

        Task<bool> DeleteLessonAsync(string lessonId);

        Task<bool> DeleteSectionAsync(string sectionId);
        Task<IEnumerable<CourseDTO>> GetPublicCoursesAsync(int limit = 10, string? category = null);

    }
}
    