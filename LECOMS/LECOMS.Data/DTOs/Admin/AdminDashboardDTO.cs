using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Admin
{
    public class AdminDashboardDTO
    {
        // Thông tin khoảng thời gian BE đã resolve
        public AdminDashboardRangeDTO Range { get; set; }

        public AdminDashboardOverviewDTO Overview { get; set; }
        public List<AdminRevenuePointDTO> RevenueChart { get; set; }
        public List<AdminTopShopDTO> TopShops { get; set; }
        public List<AdminTopProductDTO> TopProducts { get; set; }
        public List<AdminOrderMiniDTO> RecentOrders { get; set; }
        public AdminRefundSummaryDTO RefundSummary { get; set; }
        public AdminUserSummaryDTO UserSummary { get; set; }
        public AdminSystemHealthDTO SystemHealth { get; set; }
    }

    public class AdminDashboardRangeDTO
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

    public class AdminDashboardOverviewDTO
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // Orders
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int PendingOrders { get; set; }

        // Revenue
        public decimal TotalRevenue { get; set; }          // Tổng doanh thu từ các đơn hàng Paid
        public decimal PlatformFee { get; set; }           // Phí nền tảng (5% của TotalRevenue)
        public decimal TotalRefundAmount { get; set; }     // Tổng tiền hoàn trả
        public decimal NetRevenue { get; set; }            // = TotalRevenue - TotalRefundAmount
        public decimal AverageOrderValue { get; set; }     // Giá trị đơn hàng trung bình

        // Users & Shops
        public int TotalUsers { get; set; }
        public int NewUsers { get; set; }                  // Users mới trong khoảng thời gian
        public int ActiveShops { get; set; }
        public int NewShops { get; set; }                  // Shops mới trong khoảng thời gian
    }

    public class AdminRevenuePointDTO
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public decimal PlatformFee { get; set; }
        public int OrderCount { get; set; }
    }

    public class AdminTopShopDTO
    {
        public int ShopId { get; set; }
        public string ShopName { get; set; }
        public string SellerName { get; set; }
        public string ShopLogoUrl { get; set; }

        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public double AverageRating { get; set; }
        public int ProductCount { get; set; }
    }

    public class AdminTopProductDTO
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string ThumbnailUrl { get; set; }
        public string ShopName { get; set; }
        public int ShopId { get; set; }

        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageRating { get; set; }
        public int FeedbackCount { get; set; }
    }

    public class AdminOrderMiniDTO
    {
        public string OrderId { get; set; }
        public string OrderCode { get; set; }
        public DateTime CreatedAt { get; set; }

        public string Status { get; set; }
        public string PaymentStatus { get; set; }

        public decimal Total { get; set; }
        public string CustomerName { get; set; }
        public string ShopName { get; set; }
    }

    public class AdminRefundSummaryDTO
    {
        public int TotalRequests { get; set; }
        public int PendingAdminCount { get; set; }         // Chờ Admin duyệt
        public int ApprovedCount { get; set; }             // Đã duyệt
        public int RejectedCount { get; set; }             // Shop từ chối

        public decimal TotalRefundAmount { get; set; }     // Tổng tiền đã hoàn trả (Approved)
    }

    public class AdminUserSummaryDTO
    {
        public int TotalUsers { get; set; }
        public int BuyerCount { get; set; }
        public int SellerCount { get; set; }
        public int AdminCount { get; set; }

        public int NewUsersInRange { get; set; }           // Users mới trong khoảng thời gian
        public int ActiveUsersInRange { get; set; }        // Users có hoạt động (đặt hàng)
    }

    public class AdminSystemHealthDTO
    {
        // Products
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }

        // Shops
        public int TotalShops { get; set; }
        public int ActiveShops { get; set; }
        public int PendingShops { get; set; }

        // Quality
        public double AverageSystemRating { get; set; }    // Rating trung bình toàn hệ thống
        public int TotalFeedbacks { get; set; }
    }
}
