using LECOMS.Data.DTOs.Course;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
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

        public async Task<Course> CreateCourseAsync(CreateCourseDto dto)
        {
            // Optional: check Shop exists
            var shop = await _unitOfWork.Shops.GetAsync(s => s.Id == dto.ShopId);
            if (shop == null) throw new Exception("Shop not found");

            var course = new Course
            {
                Id = Guid.NewGuid().ToString(),
                Title = dto.Title,
                Slug = dto.Slug,
                Summary = dto.Summary,
                CategoryId = dto.CategoryId,
                ShopId = dto.ShopId,
                Active = 1
            };
            await _unitOfWork.Courses.AddAsync(course);
            await _unitOfWork.CompleteAsync();
            return course;
        }

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

        public async Task<Lesson> CreateLessonAsync(CreateLessonDto dto)
        {
            // 1️⃣ Validate cơ bản
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new InvalidOperationException("Lesson title is required.");

            // 2️⃣ Validate theo loại bài học
            switch (dto.Type)
            {
                case LessonType.Video:
                    if (string.IsNullOrWhiteSpace(dto.ContentUrl) || !dto.DurationSeconds.HasValue || dto.DurationSeconds <= 0)
                        throw new InvalidOperationException("Video lesson requires ContentUrl and DurationSeconds > 0.");
                    break;

                case LessonType.Text:
                    // text có thể không cần ContentUrl / DurationSeconds
                    break;

                case LessonType.Quiz:
                    // TODO: validate riêng cho bài quiz nếu cần
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported lesson type: {dto.Type}");
            }

            // 3️⃣ Tạo entity
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

            // 4️⃣ Lưu DB
            await _unitOfWork.Lessons.AddAsync(lesson);
            await _unitOfWork.CompleteAsync();
            // Reload kèm Section
            lesson = await _unitOfWork.Lessons.GetAsync(
                l => l.Id == lesson.Id,
                includeProperties: "Section"
                );

            // 👉 cắt vòng lặp: tránh serialize Section.Lessons (nếu bạn return entity)
            if (lesson?.Section?.Lessons != null)
                lesson.Section.Lessons = null;

            return lesson;
        }


        public async Task<CourseProduct> LinkCourseProductAsync(LinkCourseProductDto dto)
        {
            var exists = await _unitOfWork.CourseProducts.AnyAsync(
                cp => cp.CourseId == dto.CourseId && cp.ProductId == dto.ProductId
            );
            if (exists) throw new Exception("Course already linked with this product");

            var cp = new CourseProduct
            {
                Id = Guid.NewGuid().ToString(),
                CourseId = dto.CourseId,
                ProductId = dto.ProductId
            };
            await _unitOfWork.CourseProducts.AddAsync(cp);
            await _unitOfWork.CompleteAsync();
            return cp;
        }
    }
}