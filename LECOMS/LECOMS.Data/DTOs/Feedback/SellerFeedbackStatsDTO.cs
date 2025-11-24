using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Feedback
{
    public class SellerFeedbackStatsDTO
    {
        public int ShopId { get; set; }
        public string ShopName { get; set; }
        public double AverageRating { get; set; }
        public int TotalFeedback { get; set; }
        public int LowRatingCount { get; set; }
    }
}
