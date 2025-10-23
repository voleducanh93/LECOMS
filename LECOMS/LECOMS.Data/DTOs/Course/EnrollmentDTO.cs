
using System;

namespace LECOMS.Data.DTOs.Course
{
    public class EnrollmentDTO
    {
        public string Id { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string CourseId { get; set; } = null!;
        public string? CourseTitle { get; set; }
        public double Progress { get; set; }
        public DateTime EnrolledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}