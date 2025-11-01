using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Course
{
    public class CreateCourseDto
    {
        public string Title { get; set; } = null!;
        public string? Slug { get; set; } // ✅ optional, BE sẽ tự generate nếu null
        public string? Summary { get; set; }
        public string CategoryId { get; set; } = null!;
        public int? ShopId { get; set; } // ✅ để nullable, BE sẽ tự gán theo seller
        public string? CourseThumbnail { get; set; }
    }
}
