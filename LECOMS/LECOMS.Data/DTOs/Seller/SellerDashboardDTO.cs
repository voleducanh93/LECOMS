using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Seller
{
    public class SellerDashboardDTO
    {
        public int ShopId { get; set; }
        public string ShopName { get; set; }

        // Thông tin khoảng thời gian BE đã resolve
        public SellerDashboardRangeDTO Range { get; set; }

        public SellerDashboardOverviewDTO Overview { get; set; }
        public List<SellerRevenuePointDTO> RevenueChart { get; set; }
        public List<SellerProductStatDTO> TopProducts { get; set; }
        public List<SellerOrderMiniDTO> RecentOrders { get; set; }
        public SellerRefundSummaryDTO RefundSummary { get; set; }
        public SellerRatingSummaryDTO RatingSummary { get; set; }
        public SellerWalletSummaryDTO WalletSummary { get; set; }
    }

    public class SellerDashboardRangeDTO
    {
        /// <summary>
        /// day | week | month | quarter | year | custom
        /// </summary>
        public string View { get; set; }

        /// <summary>
        /// Ngày gốc dùng để tính (day/week/month/quarter/year)
        /// </summary>
        public DateTime BaseDate { get; set; }

        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class SellerDashboardOverviewDTO
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int PendingOrders { get; set; }

        public decimal TotalRevenue { get; set; }       // tổng tiền order Paid
        public decimal TotalRefundAmount { get; set; }  // tổng tiền refund Approved
        public decimal NetRevenue { get; set; }         // = TotalRevenue - TotalRefundAmount
        public decimal AverageOrderValue { get; set; }  // = TotalRevenue / CompletedOrders (nếu có)

        public int UniqueCustomers { get; set; }
    }

    public class SellerRevenuePointDTO
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
    }

    public class SellerProductStatDTO
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string ThumbnailUrl { get; set; }

        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }

        public double AverageRating { get; set; }
        public int FeedbackCount { get; set; }
    }

    public class SellerOrderMiniDTO
    {
        public string OrderId { get; set; }
        public string OrderCode { get; set; }
        public DateTime CreatedAt { get; set; }

        public string Status { get; set; }
        public string PaymentStatus { get; set; }

        public decimal Total { get; set; }
        public string CustomerName { get; set; }
    }

    public class SellerRefundSummaryDTO
    {
        public int TotalRequests { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }

        public decimal TotalRefundAmount { get; set; } // sum Approved
    }

    public class SellerRatingSummaryDTO
    {
        public double AverageRating { get; set; }
        public int TotalFeedbacks { get; set; }

        public int Rating1Count { get; set; }
        public int Rating2Count { get; set; }
        public int Rating3Count { get; set; }
        public int Rating4Count { get; set; }
        public int Rating5Count { get; set; }

        public double PositiveRate { get; set; } // % rating >= 4
    }

    /// <summary>
    /// Tóm tắt thông tin ví shop để FE show ở Dashboard.
    /// </summary>
    public class SellerWalletSummaryDTO
    {
        public decimal AvailableBalance { get; set; }      // Số dư khả dụng
        public decimal PendingBalance { get; set; }        // Đang giữ (chưa release)
        public decimal TotalEarned => AvailableBalance + PendingBalance;

        public decimal PendingWithdrawalAmount { get; set; }   // Đã gửi yêu cầu rút, đang chờ duyệt
        public decimal ApprovedWithdrawalAmount { get; set; }  // Tổng tiền rút đã approved (trong range)
        public DateTime? LastUpdatedAt { get; set; }           // Lần cập nhật ví gần nhất (nếu có)
    }
}
