using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class FeedbackReply
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FeedbackId { get; set; }
        public int ShopId { get; set; }

        public string ReplyContent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Nav
        public Feedback Feedback { get; set; }
        public Shop Shop { get; set; }
    }
}
