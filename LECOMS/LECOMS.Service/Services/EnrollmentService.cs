using CloudinaryDotNet;
using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Gamification;
using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IUnitOfWork _uow;

        private readonly IGamificationService _gamification;
        private readonly IAchievementService _achievement;

        public EnrollmentService(IUnitOfWork uow, IGamificationService gamification, IAchievementService achievement)
        {
            _uow = uow;
            _gamification = gamification;
            _achievement = achievement;
        }

        public async Task<EnrollmentDTO> EnrollAsync(string userId, string courseId)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("ID người dùng là bắt buộc", nameof(userId));
            if (string.IsNullOrWhiteSpace(courseId)) throw new ArgumentException("khóa học là bắt buộc", nameof(courseId));

            var course = await _uow.Courses.GetAsync(c => c.Id == courseId);
            if (course == null) throw new KeyNotFoundException("Khóa học không được tìm thấy.");

            var existing = await _uow.Enrollments.GetByUserAndCourseAsync(userId, courseId);
            if (existing != null) throw new InvalidOperationException("Người dùng đã đăng ký khóa học này.");

            var enrollment = new Enrollment
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                CourseId = courseId,
                Progress = 0,
                EnrolledAt = DateTime.UtcNow
            };

            await _uow.Enrollments.AddAsync(enrollment);
            await _uow.CompleteAsync();
            // ================================
            // ⭐ ACHIEVEMENTS — COURSE EVENTS
            // ================================
            await _achievement.IncreaseProgressAsync(userId, "ACHV_FIRST_COURSE_ENROLLMENT", 1);
            await _achievement.IncreaseProgressAsync(userId, "ACHV_5_COURSE_ENROLLMENT", 1);

            return new EnrollmentDTO
            {
                Id = enrollment.Id,
                UserId = enrollment.UserId,
                CourseId = enrollment.CourseId,
                CourseTitle = course.Title,
                CourseSlug = course.Slug,
                Progress = enrollment.Progress,
                EnrolledAt = enrollment.EnrolledAt,
                CompletedAt = enrollment.CompletedAt,
                ShopName = enrollment.Course.Shop?.Name,
                CategoryName = enrollment.Course.Category?.Name,
                CourseThumbnail = enrollment.Course.CourseThumbnail
            };
        }

        public async Task<EnrollmentDTO?> GetEnrollmentAsync(string userId, string courseId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(courseId)) return null;

            var e = await _uow.Enrollments.GetAsync(en => en.UserId == userId && en.CourseId == courseId, includeProperties: "Course,Course.Shop,Course.Category"
);
            if (e == null) return null;

            return new EnrollmentDTO
            {
                Id = e.Id,
                UserId = e.UserId,
                CourseId = e.CourseId,
                CourseTitle = e.Course?.Title,
                CourseSlug = e.Course.Slug,
                Progress = e.Progress,
                EnrolledAt = e.EnrolledAt,
                CompletedAt = e.CompletedAt,
                // NEW — cần Include Course.Shop và Course.Category ở Repository
                ShopName = e.Course.Shop?.Name,
                CategoryName = e.Course.Category?.Name,
                CourseThumbnail = e.Course.CourseThumbnail
            };
        }
        public async Task<IEnumerable<EnrollmentDTO>> GetUserEnrollmentsAsync(string userId)
        {
            var list = await _uow.Enrollments.GetByUserAsync(userId);

            return list.Select(e => new EnrollmentDTO
            {
                Id = e.Id,
                UserId = e.UserId,
                CourseId = e.CourseId,
                CourseTitle = e.Course.Title,
                CourseSlug = e.Course.Slug,
                Progress = e.Progress,
                EnrolledAt = e.EnrolledAt,
                CompletedAt = e.CompletedAt,
                ShopName = e.Course.Shop?.Name,
                CategoryName = e.Course.Category?.Name,
                CourseThumbnail = e.Course.CourseThumbnail
            });
        }
        public async Task<object> GetLearningDetailAsync(string userId, string courseId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(courseId))
                throw new ArgumentException("Invalid userId or courseId.");

            // 1) Load enrollment + Course + Shop + Category
            var enrollment = await _uow.Enrollments.GetAsync(
                e => e.UserId == userId && e.CourseId == courseId,
                includeProperties: "Course,Course.Shop,Course.Category"
            );

            if (enrollment == null)
                throw new KeyNotFoundException("Người dùng không được ghi danh vào khóa học này.");

            var course = enrollment.Course;

            // 2) Load all Sections + Lessons in 1 query
            var sections = await _uow.Sections.Query()
                .Where(s => s.CourseId == courseId)
                .Include(s => s.Lessons)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync();

            // 3) Load all user progress of this user
            var progressList = await _uow.UserLessonProgresses.Query()
                .Where(lp => lp.UserId == userId)
                .ToListAsync();

            // 4) Load ALL linked products for EVERY lesson in 1 single query
            var lessonIds = sections.SelectMany(s => s.Lessons).Select(l => l.Id).ToList();

            var allLinked = await _uow.LessonProducts.Query()
                .Where(lp => lessonIds.Contains(lp.LessonId))
                .Include(lp => lp.Product).ThenInclude(p => p.Category)
                .Include(lp => lp.Product).ThenInclude(p => p.Images)
                .Include(lp => lp.Product).ThenInclude(p => p.Shop)
                .ToListAsync();

            // COUNT PROGRESS
            var allLessons = sections.SelectMany(s => s.Lessons).ToList();
            int totalLessons = allLessons.Count;

            int completedLessons = allLessons.Count(l =>
                progressList.Any(p => p.LessonId == l.Id && p.IsCompleted)
            );

            double percent = totalLessons == 0 ? 0 : (completedLessons * 100.0 / totalLessons);

            if (percent > 100) percent = 100;

            // BUILD RESULT
            var resultSections = sections.Select(s => new
            {
                id = s.Id,
                title = s.Title,
                orderIndex = s.OrderIndex,

                lessons = s.Lessons
                    .OrderBy(l => l.OrderIndex)
                    .Select(l =>
                    {
                        var lp = progressList.FirstOrDefault(p => p.LessonId == l.Id);

                        var linked = allLinked.Where(x => x.LessonId == l.Id).ToList();

                        var linkedProducts = linked.Select(x => new
                        {
                            id = x.Product.Id,
                            name = x.Product.Name,
                            price = x.Product.Price,
                            slug = x.Product.Slug,
                            categoryId = x.Product.CategoryId,
                            categoryName = x.Product.Category?.Name,
                            categorySlug = x.Product.Category?.Slug,
                            thumbnailUrl = x.Product.Images
                                             .OrderBy(i => i.OrderIndex)
                                             .FirstOrDefault(i => i.IsPrimary)?.Url,
                            shopName = x.Product.Shop?.Name
                        }).ToList();

                        return new
                        {
                            id = l.Id,
                            title = l.Title,
                            type = l.Type.ToString(),
                            durationSeconds = l.DurationSeconds,
                            contentUrl = l.ContentUrl,
                            orderIndex = l.OrderIndex,
                            isCompleted = lp?.IsCompleted ?? false,
                            xpReward = lp?.XpEarned ?? 0,
                            linkedProducts = linkedProducts.Any() ? linkedProducts : null
                        };
                    })
            });

            return new
            {
                course = new
                {
                    id = course.Id,
                    title = course.Title,
                    summary = course.Summary,
                    thumbnail = course.CourseThumbnail,
                    shopName = course.Shop?.Name,
                    categoryName = course.Category?.Name
                },
                progress = new
                {
                    totalLessons,
                    completedLessons,
                    percent
                },
                sections = resultSections
            };
        }



        public async Task<bool> CompleteLessonAsync(string userId, string lessonId)
        {
            // 1. Load lesson + Section + Course
            var lesson = await _uow.Lessons.GetAsync(
                l => l.Id == lessonId,
                includeProperties: "Section,Section.Course"
            );

            if (lesson == null)
                throw new KeyNotFoundException("Bài học không được tìm thấy.");

            var courseId = lesson.Section.CourseId;

            // 2. Earn rule (XP reward)
            var earnRule = await _uow.EarnRules.GetAsync(
                r => r.Action == "CompleteLesson" && r.Active
            );

            int xpReward = earnRule?.Points ?? 5;

            // 3. Load or create progress
            var progress = await _uow.UserLessonProgresses.GetProgressAsync(userId, lessonId);

            if (progress == null)
            {
                progress = new UserLessonProgress
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    LessonId = lessonId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow,
                    XpEarned = xpReward
                };

                await _uow.UserLessonProgresses.AddAsync(progress);
            }
            else if (!progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.UtcNow;
                progress.XpEarned = xpReward;

                await _uow.UserLessonProgresses.UpdateAsync(progress);
            }

            // 4. Update enrollment progress
            var enrollment = await _uow.Enrollments.GetAsync(
                e => e.UserId == userId && e.CourseId == courseId,
                includeProperties: "Course,Course.Sections.Lessons"
            );

            if (enrollment != null)
            {
                // Get all lessons of this course
                var lessonIdsOfCourse = await _uow.Lessons.Query()
                    .Where(l => l.Section.CourseId == courseId)
                    .Select(l => l.Id)
                    .ToListAsync();

                int totalLessons = lessonIdsOfCourse.Count;

                // Number of completed lessons of this course only
                int completedLessons = await _uow.UserLessonProgresses.Query()
                    .Where(p => p.UserId == userId &&
                                p.IsCompleted &&
                                lessonIdsOfCourse.Contains(p.LessonId))
                    .CountAsync();

                // Calculate percent
                int percent = totalLessons == 0
                    ? 0
                    : (int)(completedLessons * 100.0 / totalLessons);

                if (percent > 100) percent = 100;

                enrollment.Progress = percent;

                // Mark course completed
                if (percent == 100 && enrollment.CompletedAt == null)
                {
                    enrollment.CompletedAt = DateTime.UtcNow;
                }

                await _uow.Enrollments.UpdateAsync(enrollment);
            }

            await _uow.CompleteAsync();

            // 5. Gamification event
            await _gamification.HandleEventAsync(userId, new GamificationEventDTO
            {
                Action = "CompleteLesson",
                ReferenceId = lessonId
            });
            // ================================
            // ⭐ ACHIEVEMENTS — LESSON EVENTS
            // ================================
            await _achievement.IncreaseProgressAsync(userId, "ACHV_FIRST_LESSON", 1);
            await _achievement.IncreaseProgressAsync(userId, "ACHV_3_LESSONS", 1);
            await _achievement.IncreaseProgressAsync(userId, "ACHV_10_LESSONS", 1);

            return true;
        }

    }
}