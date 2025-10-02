using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    [Index(nameof(UserId), nameof(CourseId), IsUnique = true)]
    public class Enrollment
    {
        [Key] public string Id { get; set; }

        [Required] public string UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        [Required] public string CourseId { get; set; }
        [ForeignKey(nameof(CourseId))] public Course Course { get; set; } = null!;

        public double Progress { get; set; } // 0..100
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
