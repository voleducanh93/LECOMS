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
        Task<object> GetPublicCoursesAsync(
            string? search = null,
            string? category = null,
            string? sort = null,
            int page = 1,
            int pageSize = 10
        );
        Task<IEnumerable<CourseDTO>> GetCoursesBySellerAsync(string sellerId);
        Task<CourseDTO?> GetCourseByIdAsync(string courseId, string sellerId);
        Task<CourseDTO> UpdateCourseAsync(string courseId, UpdateCourseDto dto, string sellerId);
        Task<bool> DeleteCourseAsync(string courseId, string sellerId);
        Task<IEnumerable<SectionDTO>> GetSectionsByCourseAsync(string courseId);
        Task<IEnumerable<LessonDto>> GetLessonsBySectionAsync(string sectionId);
        Task<CourseDTO> GetCourseBySlugAsync(string slug, string? userId);
        Task<Course> GetCourseBySlugForRecommendAsync(string slug, string? userId);
        // ⭐⭐ === THÊM 2 HÀM UPDATE === ⭐⭐
        Task<CourseSection> UpdateSectionAsync(string sectionId, CreateSectionDto dto);

        Task<Lesson> UpdateLessonAsync(string lessonId, CreateLessonDto dto);
    }
}
    