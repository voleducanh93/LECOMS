using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Feedback
{
    public class FeedbackDTO
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserAvatar { get; set; }

        public string ProductId { get; set; }
        public int ShopId { get; set; }

        public int Rating { get; set; }
        public string Content { get; set; }
        public List<string> Images { get; set; } = new();
        public DateTime CreatedAt { get; set; }

        public object? Reply { get; set; }
    }
}
