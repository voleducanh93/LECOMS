using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    [Index(nameof(Slug), IsUnique = true)]  // 👈 tạo chỉ mục unique cho Slug
    public class CourseCategory
    {
        [Key]
        public string Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [Required, MaxLength(180)]
        public string Slug { get; set; } = null!;  // 👈 BẮT BUỘC có giá trị (NOT NULL)

        public byte Active { get; set; } = 1;

        public ICollection<Course> Courses { get; set; } = new List<Course>();
        public string? Description { get; set; }   // ✅ thêm dòng này

    }
}
