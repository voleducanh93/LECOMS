using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Course
{
    public class UpdateCourseDto
    {
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? CategoryId { get; set; }
        public string? CourseThumbnail { get; set; }
        public byte? Active { get; set; }
    }
}
