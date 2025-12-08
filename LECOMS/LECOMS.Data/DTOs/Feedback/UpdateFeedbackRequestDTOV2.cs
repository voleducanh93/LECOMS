using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Feedback
{
    public class UpdateFeedbackRequestDTOV2
    {
        public int Rating { get; set; }    // 1-5
        public string Content { get; set; }

        // Nếu null => giữ nguyên ảnh cũ; nếu không null => replace toàn bộ bằng list URL mới
        public List<string>? ImageUrls { get; set; }
    }
}
