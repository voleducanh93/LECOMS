using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Feedback
{
    public class ShopFeedbackReportDTO
    {
        public int ShopId { get; set; }
        public string ShopName { get; set; }

        public double AverageRating { get; set; }
        public int TotalFeedback { get; set; }

        public Dictionary<int, int> RatingCounts { get; set; }

        public IEnumerable<FeedbackDTO> RecentFeedbacks { get; set; }
        public IEnumerable<LowRatingProductDTO> LowRatingProducts { get; set; }
    }

    public class LowRatingProductDTO
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public double AvgRating { get; set; }
        public int FeedbackCount { get; set; }
    }
}
