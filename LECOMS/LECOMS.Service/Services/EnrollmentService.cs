﻿using LECOMS.Data.DTOs.Course;
using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using System;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IUnitOfWork _uow;

        public EnrollmentService(IUnitOfWork uow)
        {
            _uow = uow;
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
                Progress = enrollment.Progress,
                EnrolledAt = enrollment.EnrolledAt,
                CompletedAt = enrollment.CompletedAt
            };
        }

        public async Task<EnrollmentDTO?> GetEnrollmentAsync(string userId, string courseId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(courseId)) return null;

            var e = await _uow.Enrollments.GetAsync(en => en.UserId == userId && en.CourseId == courseId, includeProperties: "Course");
            if (e == null) return null;

            return new EnrollmentDTO
            {
                Id = e.Id,
                UserId = e.UserId,
                CourseId = e.CourseId,
                CourseTitle = e.Course?.Title,
                Progress = e.Progress,
                EnrolledAt = e.EnrolledAt,
                CompletedAt = e.CompletedAt
            };
        }
    }
}