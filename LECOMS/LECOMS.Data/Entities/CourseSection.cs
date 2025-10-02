using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class CourseSection
    {
        [Key] public string Id { get; set; }

        [Required] public string CourseId { get; set; }
        [ForeignKey(nameof(CourseId))] public Course Course { get; set; } = null!;

        [Required, MaxLength(200)] public string Title { get; set; } = null!;
        public int OrderIndex { get; set; }

        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
