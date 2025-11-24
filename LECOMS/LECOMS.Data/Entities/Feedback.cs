using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class Feedback
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string OrderId { get; set; }
        public string UserId { get; set; }
        public int ShopId { get; set; }
        public string ProductId { get; set; }

        public int Rating { get; set; } // 1-5
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Nav
        public User User { get; set; }
        public Shop Shop { get; set; }
        public Product Product { get; set; }
        public Order Order { get; set; }

        public ICollection<FeedbackImage> Images { get; set; } = new List<FeedbackImage>();
        public FeedbackReply Reply { get; set; }
    }
}
