using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class SellerCourseService : ISellerCourseService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SellerCourseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Seller tạo khóa học mới (có thumbnail)
        /// </summary>
        public async Task<Course> CreateCourseAsync(CreateCourseDto dto)
        {
            // ✅ Kiểm tra shop tồn tại
            var shop = await _unitOfWork.Shops.GetAsync(s => s.Id == dto.ShopId);
            if (shop == null)
                throw new InvalidOperationException("Shop not found.");

            // ✅ Sinh slug tự động nếu chưa có
            var slug = string.IsNullOrWhiteSpace(dto.Slug)
                ? GenerateSlug(dto.Title)
                : dto.Slug;

            var course = new Course
            {
                Id = Guid.NewGuid().ToString(),
                Title = dto.Title,
                Slug = slug,
                Summary = dto.Summary,
                CategoryId = dto.CategoryId,
                ShopId = dto.ShopId,
                CourseThumbnail = dto.CourseThumbnail,
                Active = 1
            };

            await _unitOfWork.Courses.AddAsync(course);
            await _unitOfWork.CompleteAsync();
            return course;
        }

        /// <summary>
        /// Seller tạo Section (phần trong khóa học)
        /// </summary>
        public async Task<CourseSection> CreateSectionAsync(CreateSectionDto dto)
        {
            var section = new CourseSection
            {
                Id = Guid.NewGuid().ToString(),
                CourseId = dto.CourseId,
                Title = dto.Title,
                OrderIndex = dto.OrderIndex
            };

            await _unitOfWork.Sections.AddAsync(section);
            await _unitOfWork.CompleteAsync();
            return section;
        }

        /// <summary>
        /// Seller tạo bài học (Lesson) trong section
        /// </summary>
        public async Task<Lesson> CreateLessonAsync(CreateLessonDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new InvalidOperationException("Lesson title is required.");

            // Validate theo loại bài học
            switch (dto.Type)
            {
                case LessonType.Video:
                    if (string.IsNullOrWhiteSpace(dto.ContentUrl) ||
                        !dto.DurationSeconds.HasValue ||
                        dto.DurationSeconds <= 0)
                        throw new InvalidOperationException("Video lesson requires ContentUrl and DurationSeconds > 0.");
                    break;
                case LessonType.Text:
                case LessonType.Quiz:
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported lesson type: {dto.Type}");
            }

            var lesson = new Lesson
            {
                Id = Guid.NewGuid().ToString(),
                CourseSectionId = dto.CourseSectionId,
                Title = dto.Title.Trim(),
                Type = dto.Type,
                DurationSeconds = dto.DurationSeconds,
                ContentUrl = dto.ContentUrl?.Trim(),
                OrderIndex = dto.OrderIndex
            };

            await _unitOfWork.Lessons.AddAsync(lesson);
            await _unitOfWork.CompleteAsync();

            // Load lại để include Section (nếu cần)
            lesson = await _unitOfWork.Lessons.GetAsync(
                l => l.Id == lesson.Id,
                includeProperties: "Section"
            );

            if (lesson?.Section?.Lessons != null)
                lesson.Section.Lessons = null;

            return lesson;
        }

        /// <summary>
        /// Seller liên kết bài học (Lesson) với sản phẩm
        /// </summary>
        public async Task<LessonProduct> LinkLessonProductAsync(LinkLessonProductDto dto)
        {
            var exists = await _unitOfWork.LessonProducts.ExistsAsync(dto.LessonId, dto.ProductId);
            if (exists)
                throw new InvalidOperationException("Lesson already linked with this product.");

            var entity = new LessonProduct
            {
                Id = Guid.NewGuid().ToString(),
                LessonId = dto.LessonId,
                ProductId = dto.ProductId
            };

            await _unitOfWork.LessonProducts.AddAsync(entity);
            await _unitOfWork.CompleteAsync();
            return entity;
        }

        /// <summary>
        /// Seller hủy liên kết giữa Lesson và Product
        /// </summary>
        public async Task<bool> UnlinkLessonProductAsync(string lessonId, string productId)
        {
            var record = await _unitOfWork.LessonProducts.GetAsync(
                lp => lp.LessonId == lessonId && lp.ProductId == productId
            );
            if (record == null) return false;

            await _unitOfWork.LessonProducts.DeleteAsync(record);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        /// <summary>
        /// Seller xóa một bài học (Lesson) và các liên kết Lesson–Product
        /// </summary>
        public async Task<bool> DeleteLessonAsync(string lessonId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var lesson = await _unitOfWork.Lessons.GetAsync(l => l.Id == lessonId);
                if (lesson == null) return false;

                // Xóa liên kết Lesson–Product trước
                var links = await _unitOfWork.LessonProducts.GetAllAsync(lp => lp.LessonId == lessonId);
                foreach (var link in links)
                    await _unitOfWork.LessonProducts.DeleteAsync(link);

                await _unitOfWork.Lessons.DeleteAsync(lesson);
                await _unitOfWork.CompleteAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Seller xóa toàn bộ Section và các Lesson bên trong (kèm liên kết sản phẩm)
        /// </summary>
        public async Task<bool> DeleteSectionAsync(string sectionId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var section = await _unitOfWork.Sections.GetAsync(
                    s => s.Id == sectionId,
                    includeProperties: "Lessons"
                );
                if (section == null) return false;

                foreach (var lesson in section.Lessons.ToList())
                {
                    var links = await _unitOfWork.LessonProducts.GetAllAsync(lp => lp.LessonId == lesson.Id);
                    foreach (var link in links)
                        await _unitOfWork.LessonProducts.DeleteAsync(link);

                    await _unitOfWork.Lessons.DeleteAsync(lesson);
                }

                await _unitOfWork.Sections.DeleteAsync(section);
                await _unitOfWork.CompleteAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Helper: sinh slug
        private static string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.ToLowerInvariant();
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]+", "-").Trim('-');
            return s;
        }
        public async Task<IEnumerable<CourseDTO>> GetPublicCoursesAsync(int limit = 10, string? category = null)
        {
            string? categoryId = null;

            // Nếu có slug danh mục → tìm CategoryId tương ứng
            if (!string.IsNullOrEmpty(category))
            {
                var categoryEntity = await _unitOfWork.CourseCategories.GetAsync(
                    c => c.Slug == category && c.Active == 1
                );

                if (categoryEntity == null)
                    return Enumerable.Empty<CourseDTO>();

                categoryId = categoryEntity.Id;
            }

            // Lấy danh sách khóa học public
            var courses = await _unitOfWork.Courses.GetAllAsync(
                filter: c => c.Active == 1 &&
                             (string.IsNullOrEmpty(categoryId) || c.CategoryId == categoryId),
                includeProperties: "Category,Shop"
            );

            var result = courses
                .OrderByDescending(c => c.Id)
                .Take(limit)
                .Select(c => new CourseDTO
                {
                    Id = c.Id,
                    Title = c.Title,
                    Slug = c.Slug,
                    Summary = c.Summary,
                    CategoryId = c.CategoryId,
                    CategoryName = c.Category?.Name,
                    ShopId = c.ShopId,
                    ShopName = c.Shop?.Name,
                    CourseThumbnail = c.CourseThumbnail,
                    Active = c.Active
                });

            return result;
        }

    }
}
