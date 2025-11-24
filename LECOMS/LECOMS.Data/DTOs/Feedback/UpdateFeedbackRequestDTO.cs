using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Feedback
{
    public class UpdateFeedbackRequestDTO
    {
        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
        /// <summary>
        /// Danh sách URL ảnh mới.
        /// Nếu = null => giữ nguyên ảnh cũ
        /// Nếu là list rỗng => xóa hết ảnh
        /// </summary>
        public List<string>? ImageUrls { get; set; }
    }

    public class UpdateReplyFeedbackRequestDTO
    {
        public string ReplyContent { get; set; } = string.Empty;
    }
}
