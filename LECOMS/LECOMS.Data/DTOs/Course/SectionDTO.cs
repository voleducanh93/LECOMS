using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Course
{
    public class SectionDTO
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int OrderIndex { get; set; }
        public List<LessonDto> Lessons { get; set; } = new();
        public ApprovalStatus ApprovalStatus { get; set; }
        public string? ModeratorNote { get; set; }

    }
}
