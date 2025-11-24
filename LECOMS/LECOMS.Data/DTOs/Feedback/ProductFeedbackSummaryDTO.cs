using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Feedback
{
    public class ProductFeedbackSummaryDTO
    {
        public string ProductId { get; set; }
        public double AverageRating { get; set; }
        public int TotalFeedbackCount { get; set; }

        public int Rating1Count { get; set; }
        public int Rating2Count { get; set; }
        public int Rating3Count { get; set; }
        public int Rating4Count { get; set; }
        public int Rating5Count { get; set; }

        public double PositiveRate { get; set; } // % feedback >= 4*

        public IEnumerable<FeedbackDTO> RecentFeedbacks { get; set; }
    }
}
