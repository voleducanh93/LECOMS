using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    public class UserLessonProgress
    {
        [Key]
        public string Id { get; set; }

        [Required]
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        [Required]
        public string LessonId { get; set; }
        [ForeignKey(nameof(LessonId))]
        public Lesson Lesson { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        public int XpEarned { get; set; } = 0; // XP sau khi complete lesson
    }
}
