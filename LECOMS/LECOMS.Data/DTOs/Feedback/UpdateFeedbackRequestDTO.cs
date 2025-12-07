using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Feedback
{
    public class UpdateFeedbackRequestDTO
    {
        public int Rating { get; set; }    // 1-5
        public string Content { get; set; }

        // Upload ảnh mới trực tiếp (sẽ replace toàn bộ ảnh cũ)
        public List<IFormFile>? Images { get; set; }
    }

    public class UpdateReplyFeedbackRequestDTO
    {
        public string ReplyContent { get; set; } = string.Empty;
    }
}
