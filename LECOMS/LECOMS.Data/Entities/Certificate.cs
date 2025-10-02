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
    public class Certificate
    {
        [Key] public string Id { get; set; }

        [Required] public string UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        [Required] public string CourseId { get; set; }
        [ForeignKey(nameof(CourseId))] public Course Course { get; set; } = null!;

        [MaxLength(50)] public string Code { get; set; } = Guid.NewGuid().ToString("N")[..12].ToUpper();
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    }
}
