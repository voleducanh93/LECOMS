using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Feedback
{
    public class AdminFeedbackDashboardDTO
    {
        public int TotalFeedback { get; set; }
        public double AverageRating { get; set; }
        public double PositiveRate { get; set; }
        public double NegativeRate { get; set; }

        public IEnumerable<FeedbackDTO> RecentFeedbacks { get; set; }
        public IEnumerable<FeedbackDTO> RecentLowRatings { get; set; }

        public IEnumerable<TopRatedProductDTO> BestRatedProducts { get; set; }
        public IEnumerable<TopRatedProductDTO> WorstRatedProducts { get; set; }

        public IEnumerable<SellerFeedbackStatsDTO> SellerWithWorstRating { get; set; }
    }
}
