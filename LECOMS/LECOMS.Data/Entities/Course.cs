using Org.BouncyCastle.Tls;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class Course
    {
        [Key] public string Id { get; set; }

        [Required, MaxLength(200)] public string Title { get; set; } = null!;
        [Required, MaxLength(220)] public string Slug { get; set; } = null!;
        [MaxLength(1000)] public string? Summary { get; set; }

        // NEW
        [MaxLength(1000)] public string? CourseThumbnail { get; set; }

        [Required] public string CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))] public CourseCategory Category { get; set; } = null!;
        [Required] public int ShopId { get; set; }
        [ForeignKey(nameof(ShopId))] public Shop Shop { get; set; } = null!;
        public byte Active { get; set; } = 1;

        public ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<CourseProduct> CourseProducts { get; set; } = new List<CourseProduct>();
    }
}
