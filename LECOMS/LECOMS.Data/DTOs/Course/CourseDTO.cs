using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Course
{
    public class CourseDTO
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string? Summary { get; set; }
        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ShopId { get; set; }
        public string ShopName { get; set; }
        public string? ShopAvatar { get; set; }

        public string? CourseThumbnail { get; set; }
        public byte Active { get; set; }
    }
}
