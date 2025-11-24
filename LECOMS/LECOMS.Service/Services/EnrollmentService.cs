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

        public EnrollmentService(IUnitOfWork uow, IGamificationService gamification)
        {
            _uow = uow;
            _gamification = gamification;

        }

        public async Task<EnrollmentDTO> EnrollAsync(string userId, string courseId)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId is required", nameof(userId));
            if (string.IsNullOrWhiteSpace(courseId)) throw new ArgumentException("courseId is required", nameof(courseId));

            var course = await _uow.Courses.GetAsync(c => c.Id == courseId);
            if (course == null) throw new KeyNotFoundException("Course not found.");

            var existing = await _uow.Enrollments.GetByUserAndCourseAsync(userId, courseId);
            if (existing != null) throw new InvalidOperationException("User already enrolled in this course.");

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
            var enrollment = await _uow.Enrollments.GetAsync(
                e => e.UserId == userId && e.CourseId == courseId,
                includeProperties: "Course,Course.Shop,Course.Category"
            );

            if (enrollment == null)
                throw new KeyNotFoundException("User is not enrolled in this course.");

            var course = enrollment.Course;

            // Load Sections + Lessons
            var sections = await _uow.Sections.Query()
                .Where(s => s.CourseId == courseId)
                .Include(s => s.Lessons)
                .ToListAsync();

            course.Sections = sections;

            // Load user lesson progress
            var progressList = await _uow.UserLessonProgresses.Query()
                .Where(lp => lp.UserId == userId)
                .ToListAsync();

            var allLessons = sections.SelectMany(s => s.Lessons).ToList();

            int totalLessons = allLessons.Count;
            int completedLessons = allLessons.Count(l =>
                progressList.Any(p => p.LessonId == l.Id && p.IsCompleted));

            double percent = totalLessons == 0 ? 0 : (completedLessons * 100.0 / totalLessons);

            // Build final result with linked products
            var resultSections = await Task.WhenAll(
                sections
                    .OrderBy(s => s.OrderIndex)
                    .Select(async s => new
                    {
                        id = s.Id,
                        title = s.Title,
                        orderIndex = s.OrderIndex,

                        lessons = await Task.WhenAll(
                            s.Lessons
                                .OrderBy(l => l.OrderIndex)
                                .Select(async l =>
                                {
                                    var lp = progressList.FirstOrDefault(p => p.LessonId == l.Id);

                                    var linked = await _uow.LessonProducts.GetAllAsync(
                                        x => x.LessonId == l.Id,
                                        includeProperties: "Product,Product.Category,Product.Images,Product.Shop"
                                    );

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

                                        // ⭐ PRODUCTS FOR THIS LESSON
                                        linkedProducts = linkedProducts.Any() ? linkedProducts : null
                                    };
                                })
                        )
                    })
            );

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
            // 1. Load lesson
            var lesson = await _uow.Lessons.GetAsync(
                l => l.Id == lessonId,
                includeProperties: "Section"
            );

            if (lesson == null)
                throw new KeyNotFoundException("Lesson not found.");

            // 2. Load or create progress
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
                    XpEarned = 5
                };

                await _uow.UserLessonProgresses.AddAsync(progress);
            }
            else
            {
                if (!progress.IsCompleted)
                {
                    progress.IsCompleted = true;
                    progress.CompletedAt = DateTime.UtcNow;
                    progress.XpEarned = 5;

                    await _uow.UserLessonProgresses.UpdateAsync(progress);
                }
            }

            // 3. Update course progress
            var courseId = lesson.Section.CourseId;

            var enrollment = await _uow.Enrollments.GetAsync(
                e => e.UserId == userId && e.CourseId == courseId,
                includeProperties: "Course,Course.Sections.Lessons"
            );

            if (enrollment != null)
            {
                var allLessons = enrollment.Course.Sections.SelectMany(s => s.Lessons).Count();

                var completedLessons = await _uow.UserLessonProgresses.Query()
                    .Where(p => p.UserId == userId && p.IsCompleted)
                    .CountAsync();

                enrollment.Progress = (int)((double)completedLessons / allLessons * 100);
                await _uow.Enrollments.UpdateAsync(enrollment);
            }

            await _uow.CompleteAsync();

            // ===============================
            // ⭐ GAMIFICATION EVENT HERE ⭐
            // ===============================
            await _gamification.HandleEventAsync(userId, new GamificationEventDTO
            {
                Action = "CompleteLesson"
            });

            return true;
        }



    }
}