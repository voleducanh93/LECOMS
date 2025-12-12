using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface ISellerCourseService
    {
        // ==========================
        // COURSE
        // ==========================
        Task<Course> CreateCourseAsync(CreateCourseDto dto);

        Task<CourseDTO?> GetCourseByIdAsync(string courseId, string sellerId);

        Task<IEnumerable<CourseDTO>> GetCoursesBySellerAsync(string sellerId);

        Task<CourseDTO> UpdateCourseAsync(string courseId, UpdateCourseDto dto, string sellerId);

        Task<bool> DeleteCourseAsync(string courseId, string sellerId);

        Task<object> GetPublicCoursesAsync(
            string? search = null,
            string? category = null,
            string? sort = null,
            int page = 1,
            int pageSize = 10
        );

        Task<CourseDTO> GetCourseBySlugAsync(string slug, string? userId);

        Task<Course> GetCourseBySlugForRecommendAsync(string slug, string? userId);


        // ==========================
        // SECTION
        // ==========================
        Task<CourseSection> CreateSectionAsync(CreateSectionDto dto);

        /// <summary>
        /// Lấy Section theo Course.
        /// - Seller: thấy tất cả (kể cả Pending / Rejected)
        /// - Customer: chỉ thấy Approved
        /// </summary>
        Task<IEnumerable<SectionDTO>> GetSectionsByCourseAsync(string courseId, bool isSellerOwner = false);

        Task<CourseSection> UpdateSectionAsync(string sectionId, CreateSectionDto dto);

        Task<bool> DeleteSectionAsync(string sectionId);


        // ==========================
        // LESSON
        // ==========================
        Task<Lesson> CreateLessonAsync(CreateLessonDto dto);

        /// <summary>
        /// Lấy Lesson theo Section
        /// - Seller: thấy tất cả
        /// - Customer: chỉ thấy Approved
        /// </summary>
        Task<IEnumerable<LessonDto>> GetLessonsBySectionAsync(string sectionId, bool isSellerOwner = false);

        Task<Lesson> UpdateLessonAsync(string lessonId, CreateLessonDto dto);

        Task<bool> DeleteLessonAsync(string lessonId);


        // ==========================
        // LESSON - PRODUCT MAPPING
        // ==========================
        Task<LessonProduct> LinkLessonProductAsync(LinkLessonProductDto dto);

        Task<bool> UnlinkLessonProductAsync(string lessonId, string productId);
    }
}
