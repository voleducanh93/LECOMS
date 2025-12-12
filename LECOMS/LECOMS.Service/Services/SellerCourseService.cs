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
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new InvalidOperationException("Course title is required.");

            // 🔥 ShopId do controller gán
            var shopId = dto.ShopId ?? throw new InvalidOperationException("ShopId is missing.");

            var shop = await _unitOfWork.Shops.GetAsync(s => s.Id == shopId);
            if (shop == null)
                throw new InvalidOperationException("Không tìm thấy cửa hàng.");

            // 🔥 Sinh slug unique
            var baseSlug = GenerateSlug(dto.Title);
            var slug = await GenerateUniqueSlugAsync(baseSlug);

            var course = new Course
            {
                Id = Guid.NewGuid().ToString(),
                Title = dto.Title.Trim(),
                Slug = slug,
                Summary = dto.Summary,
                CategoryId = dto.CategoryId,
                ShopId = shopId,
                CourseThumbnail = dto.CourseThumbnail,
                Active = 1,

                // ⭐ trạng thái duyệt mặc định
                ApprovalStatus = ApprovalStatus.Pending,
                ModeratorNote = null
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
                OrderIndex = dto.OrderIndex,
                ApprovalStatus = ApprovalStatus.Pending // ⭐

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
                OrderIndex = dto.OrderIndex,
                ApprovalStatus = ApprovalStatus.Pending // ⭐

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

        /// <summary>
        /// Lấy danh sách khóa học public (search/filter/sort/paging)
        /// Chỉ trả về course đã được duyệt (Approved)
        /// </summary>
        public async Task<object> GetPublicCoursesAsync(
            string? search = null,
            string? category = null,
            string? sort = null,
            int page = 1,
            int pageSize = 10
        )
        {
            // Base query: CHỈ lấy Active + Approved
            IQueryable<Course> query = _unitOfWork.Courses.Query()
                .Include(c => c.Category)
                .Include(c => c.Shop)
                .Where(c => c.Active == 1 && c.ApprovalStatus == ApprovalStatus.Approved);

            // 🔍 Search theo tiêu đề, mô tả, hoặc tên shop
            if (!string.IsNullOrWhiteSpace(search))
            {
                string lower = search.ToLower();
                query = query.Where(c =>
                    c.Title.ToLower().Contains(lower) ||
                    (c.Summary != null && c.Summary.ToLower().Contains(lower)) ||
                    (c.Shop != null && c.Shop.Name.ToLower().Contains(lower))
                );
            }

            // 🏷️ Lọc theo category slug
            if (!string.IsNullOrEmpty(category))
            {
                var cat = await _unitOfWork.CourseCategories.GetAsync(
                    c => c.Slug == category && c.Active == 1
                );
                if (cat != null)
                    query = query.Where(c => c.CategoryId == cat.Id);
            }

            // 🔽 Sort (ưu tiên title, category, newest)
            query = sort?.ToLower() switch
            {
                "name_asc" => query.OrderBy(c => c.Title),
                "name_desc" => query.OrderByDescending(c => c.Title),
                "oldest" => query.OrderBy(c => c.Id), // giả sử Id sinh tuần tự
                _ => query.OrderByDescending(c => c.Id) // default: newest
            };

            // 📄 Pagination
            int totalItems = await query.CountAsync();
            var courses = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            // ✅ Map sang DTO
            var items = courses.Select(c => new CourseDTO
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                Summary = c.Summary,
                CategoryId = c.CategoryId,
                CategoryName = c.Category?.Name,
                ShopId = c.ShopId,
                ShopName = c.Shop?.Name,
                ShopAvatar = c.Shop?.ShopAvatar,
                CourseThumbnail = c.CourseThumbnail,
                Active = c.Active
            });

            // ✅ Trả về object chứa meta-data
            return new
            {
                totalItems,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                items
            };
        }

        public async Task<IEnumerable<CourseDTO>> GetCoursesBySellerAsync(string sellerId)
        {
            // Lấy shop theo seller
            var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == sellerId);
            if (shop == null)
                throw new InvalidOperationException("Không tìm thấy cửa hàng.");

            var courses = await _unitOfWork.Courses.Query()
                .Include(c => c.Category)
                .Include(c => c.Shop)
                .Where(c => c.ShopId == shop.Id)
                .ToListAsync();

            // Seller thấy tất cả course của mình (kể cả Pending/Rejected)
            return courses.Select(c => new CourseDTO
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                Summary = c.Summary,
                CategoryId = c.CategoryId,
                CategoryName = c.Category.Name,
                ShopId = c.ShopId,
                ShopName = c.Shop.Name,
                ShopAvatar = c.Shop.ShopAvatar,
                CourseThumbnail = c.CourseThumbnail,
                Active = c.Active
            });
        }

        public async Task<CourseDTO?> GetCourseByIdAsync(string courseId, string sellerId)
        {
            var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == sellerId);
            if (shop == null) throw new InvalidOperationException("Không tìm thấy cửa hàng.");

            var course = await _unitOfWork.Courses.GetAsync(
                c => c.Id == courseId && c.ShopId == shop.Id,
                includeProperties: "Category,Shop,Sections.Lessons"
            );

            if (course == null) return null;

            return new CourseDTO
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug,
                Summary = course.Summary,
                CategoryId = course.CategoryId,
                CategoryName = course.Category.Name,
                ShopId = course.ShopId,
                ShopName = course.Shop.Name,
                CourseThumbnail = course.CourseThumbnail,
                Active = course.Active
            };
        }

        public async Task<CourseDTO> UpdateCourseAsync(string courseId, UpdateCourseDto dto, string sellerId)
        {
            var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == sellerId);
            if (shop == null) throw new InvalidOperationException("Không tìm thấy cửa hàng.");

            var course = await _unitOfWork.Courses.GetAsync(c => c.Id == courseId && c.ShopId == shop.Id);
            if (course == null) throw new KeyNotFoundException("Course không tìm thấy.");

            // Seller có quyền update dù Active = 0 (xoá mềm)
            if (!string.IsNullOrEmpty(dto.Title))
                course.Title = dto.Title.Trim();

            if (!string.IsNullOrEmpty(dto.Summary))
                course.Summary = dto.Summary.Trim();

            if (!string.IsNullOrEmpty(dto.CategoryId))
                course.CategoryId = dto.CategoryId;

            if (!string.IsNullOrEmpty(dto.CourseThumbnail))
                course.CourseThumbnail = dto.CourseThumbnail;

            // ⭐ Cho phép khôi phục course đã xoá
            if (dto.Active.HasValue)
                course.Active = dto.Active.Value;

            // ⭐ Mỗi lần seller update → bắt buộc Pending duyệt
            course.ApprovalStatus = ApprovalStatus.Pending;
            course.ModeratorNote = null;

            await _unitOfWork.Courses.UpdateAsync(course);
            await _unitOfWork.CompleteAsync();

            return new CourseDTO
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug,
                Summary = course.Summary,
                CategoryId = course.CategoryId,
                CategoryName = course.Category?.Name,
                ShopId = course.ShopId,
                ShopName = course.Shop?.Name,
                CourseThumbnail = course.CourseThumbnail,
                Active = course.Active
            };
        }


        public async Task<bool> DeleteCourseAsync(string courseId, string sellerId)
        {
            var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == sellerId);
            if (shop == null) throw new InvalidOperationException("Không tìm thấy cửa hàng.");

            var course = await _unitOfWork.Courses.GetAsync(c => c.Id == courseId && c.ShopId == shop.Id);
            if (course == null) return false;

            // Xóa mềm (disable)
            course.Active = 0;
            await _unitOfWork.Courses.UpdateAsync(course);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        private async Task<string> GenerateUniqueSlugAsync(string baseSlug)
        {
            string slug = baseSlug;
            int counter = 2;

            while (await _unitOfWork.Courses.AnyAsync(c => c.Slug == slug))
            {
                slug = $"{baseSlug}-{counter++}";
            }

            return slug;
        }

        public async Task<IEnumerable<SectionDTO>> GetSectionsByCourseAsync(string courseId, bool isSellerOwner = false)
        {
            var sections = await _unitOfWork.Sections.GetAllAsync(
                s => s.CourseId == courseId,
                includeProperties: "Lessons.LessonProducts.Product.Shop,Lessons.LessonProducts.Product.Images"
            );

            if (!isSellerOwner)
            {
                sections = sections
                    .Where(s => s.ApprovalStatus == ApprovalStatus.Approved)
                    .ToList();
            }

            var result = new List<SectionDTO>();

            foreach (var section in sections.OrderBy(s => s.OrderIndex))
            {
                var sectionDto = new SectionDTO
                {
                    Id = section.Id,
                    Title = section.Title,
                    OrderIndex = section.OrderIndex,
                    ApprovalStatus = section.ApprovalStatus,
                    ModeratorNote = section.ModeratorNote,
                    Lessons = new List<LessonDto>()
                };

                var lessons = section.Lessons;

                if (!isSellerOwner)
                {
                    lessons = lessons
                        .Where(l => l.ApprovalStatus == ApprovalStatus.Approved)
                        .ToList();
                }

                foreach (var lesson in lessons.OrderBy(l => l.OrderIndex))
                {
                    sectionDto.Lessons.Add(new LessonDto
                    {
                        Id = lesson.Id,
                        Title = lesson.Title,
                        Type = lesson.Type,
                        ContentUrl = lesson.ContentUrl,
                        DurationSeconds = lesson.DurationSeconds,
                        OrderIndex = lesson.OrderIndex,
                        ApprovalStatus = lesson.ApprovalStatus,
                        ModeratorNote = lesson.ModeratorNote,

                        LinkedProducts = lesson.LessonProducts.Select(lp => new LessonLinkedProductDTO
                        {
                            Id = lp.Product.Id,
                            Name = lp.Product.Name,
                            Price = lp.Product.Price,

                            // ⭐ Không dùng helper — inline logic
                            ThumbnailUrl = lp.Product.Images
                                .OrderByDescending(i => i.IsPrimary)
                                .ThenBy(i => i.OrderIndex)
                                .Select(i => i.Url)
                                .FirstOrDefault(),

                            ShopName = lp.Product.Shop?.Name
                        }).ToList()
                    });
                }

                result.Add(sectionDto);
            }

            return result;
        }

        public async Task<IEnumerable<LessonDto>> GetLessonsBySectionAsync(string sectionId, bool isSellerOwner = false)
        {
            var lessons = await _unitOfWork.Lessons.GetAllAsync(
                l => l.CourseSectionId == sectionId,
                includeProperties: "LessonProducts.Product.Shop,LessonProducts.Product.Images"
            );

            if (!isSellerOwner)
            {
                lessons = lessons
                    .Where(l => l.ApprovalStatus == ApprovalStatus.Approved)
                    .ToList();
            }

            return lessons
                .OrderBy(l => l.OrderIndex)
                .Select(l => new LessonDto
                {
                    Id = l.Id,
                    CourseSectionId = l.CourseSectionId,
                    Title = l.Title,
                    Type = l.Type,
                    DurationSeconds = l.DurationSeconds,
                    ContentUrl = l.ContentUrl,
                    OrderIndex = l.OrderIndex,
                    ApprovalStatus = l.ApprovalStatus,
                    ModeratorNote = l.ModeratorNote,

                    LinkedProducts = l.LessonProducts.Select(lp => new LessonLinkedProductDTO
                    {
                        Id = lp.Product.Id,
                        Name = lp.Product.Name,
                        Price = lp.Product.Price,

                        // ⭐ Không dùng helper — inline logic
                        ThumbnailUrl = lp.Product.Images
                            .OrderByDescending(i => i.IsPrimary)
                            .ThenBy(i => i.OrderIndex)
                            .Select(i => i.Url)
                            .FirstOrDefault(),

                        ShopName = lp.Product.Shop?.Name
                    }).ToList()
                });
        }


        /// <summary>
        /// Lấy course public theo slug (chỉ Approved)
        /// </summary>
        public async Task<CourseDTO> GetCourseBySlugAsync(string slug, string? userId)
        {
            var course = await _unitOfWork.Courses.GetAsync(
                c => c.Slug == slug && c.Active == 1,
                includeProperties: "Category,Shop"
            );

            if (course == null)
                throw new KeyNotFoundException("Course không tồn tại.");

            bool isSellerOwner = (userId != null && course.Shop.SellerId == userId);

            // ⭐ Customer chỉ được xem khi Approved
            if (!isSellerOwner && course.ApprovalStatus != ApprovalStatus.Approved)
                throw new UnauthorizedAccessException("Course chưa được duyệt.");

            return new CourseDTO
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug,
                Summary = course.Summary,
                CategoryId = course.CategoryId,
                CategoryName = course.Category?.Name,
                ShopId = course.ShopId,
                ShopName = course.Shop?.Name,
                ShopAvatar = course.Shop?.ShopAvatar,
                CourseThumbnail = course.CourseThumbnail,
                Active = course.Active
            };
        }
        public async Task<Course> GetCourseBySlugForRecommendAsync(string slug, string? userId)
        {
            var course = await _unitOfWork.Courses.GetAsync(
                c => c.Slug == slug && c.Active == 1,
                includeProperties: "Shop"
            );

            if (course == null)
                throw new KeyNotFoundException("Course không tồn tại.");

            // ⭐ Seller luôn xem được
            bool isSellerOwner = (userId != null && course.Shop.SellerId == userId);

            // ⭐ Chỉ nếu là Customer bình thường -> enforce Approved
            if (!isSellerOwner && course.ApprovalStatus != ApprovalStatus.Approved)
                throw new UnauthorizedAccessException("Course chưa được duyệt.");

            return course;
        }
        public async Task<CourseSection> UpdateSectionAsync(string sectionId, CreateSectionDto dto)
        {
            var section = await _unitOfWork.Sections.GetAsync(s => s.Id == sectionId);
            if (section == null) throw new KeyNotFoundException("Section không tồn tại.");

            if (!string.IsNullOrWhiteSpace(dto.Title))
                section.Title = dto.Title.Trim();

            section.OrderIndex = dto.OrderIndex;

            // ⭐ CHUYỂN VỀ PENDING KHI SELLER UPDATE
            section.ApprovalStatus = ApprovalStatus.Pending;
            section.ModeratorNote = null;

            await _unitOfWork.Sections.UpdateAsync(section);
            await _unitOfWork.CompleteAsync();

            return section;
        }

        public async Task<Lesson> UpdateLessonAsync(string lessonId, CreateLessonDto dto)
        {
            var lesson = await _unitOfWork.Lessons.GetAsync(l => l.Id == lessonId);
            if (lesson == null) throw new KeyNotFoundException("Lesson không tồn tại.");

            lesson.Title = dto.Title.Trim();
            lesson.Type = dto.Type;
            lesson.DurationSeconds = dto.DurationSeconds;
            lesson.ContentUrl = dto.ContentUrl?.Trim();
            lesson.OrderIndex = dto.OrderIndex;

            // ⭐ CHUYỂN VỀ PENDING KHI SELLER UPDATE
            lesson.ApprovalStatus = ApprovalStatus.Pending;
            lesson.ModeratorNote = null;

            await _unitOfWork.Lessons.UpdateAsync(lesson);
            await _unitOfWork.CompleteAsync();

            return lesson;
        }


    }
}
