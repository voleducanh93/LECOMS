using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class Lesson
    {
        [Key] public string Id { get; set; }

        [Required] public string CourseSectionId { get; set; }
        [ForeignKey(nameof(CourseSectionId))] public CourseSection Section { get; set; } = null!;

        [Required, MaxLength(200)] public string Title { get; set; } = null!;
        public LessonType Type { get; set; }
        public int? DurationSeconds { get; set; } // for video
        [MaxLength(1000)] public string? ContentUrl { get; set; }
        public int OrderIndex { get; set; }
        public ICollection<LessonProduct> LessonProducts { get; set; } = new List<LessonProduct>();
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
        public string? ModeratorNote { get; set; }


    }
}
