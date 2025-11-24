using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Feedback
{
    public class CreateFeedbackRequestDTO
    {
        public string OrderId { get; set; }
        public string ProductId { get; set; }
        public int Rating { get; set; }    // 1-5
        public string Content { get; set; }

        // FE sẽ upload ảnh trước, rồi gửi list URL vào
        public List<string>? ImageUrls { get; set; }
    }
}
