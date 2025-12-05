using LECOMS.Data.DTOs.Admin;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IUnitOfWork _uow;
        private readonly UserManager<Data.Entities.User> _userManager;

        public AdminDashboardService(
            IUnitOfWork uow,
            UserManager<Data.Entities.User> userManager)
        {
            _uow = uow;
            _userManager = userManager;
        }

        public async Task<AdminDashboardDTO> GetAdminDashboardAsync(
            DateTime from,
            DateTime to)
        {
            var fromDate = from;
            var toDate = to;

            // ============================================================
            // LẤY DỮ LIỆU CƠ BẢN
            // ============================================================

            // 1. Orders trong khoảng thời gian
            var orders = await _uow.Orders.GetAllAsync(
                o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate,
                includeProperties: "User,Shop,Shop.Seller,Details,Details.Product,Details.Product.Images"
            );

            // 2. Refund requests trong khoảng
            var refundRequests = await _uow.RefundRequests.GetAllAsync(
                r => r.RequestedAt >= fromDate && r.RequestedAt <= toDate,
                includeProperties: "Order"
            );

            // 3. Users trong hệ thống (lấy tất cả để tính thống kê)
            var allUsers = await _uow.Users.GetAllUsersAsync();

            // 4.  Shops trong hệ thống
            var allShops = await _uow.Shops.GetAllAsync(includeProperties: "Seller");

            // 5. Products trong hệ thống
            var allProducts = await _uow.Products.GetAllAsync();

            // 6. Feedbacks trong hệ thống
            var feedbacks = await _uow.Feedbacks.GetAllAsync(
                f => f.CreatedAt >= fromDate && f.CreatedAt <= toDate
            );

            var allFeedbacks = await _uow.Feedbacks.GetAllAsync();

            // ============================================================
            // OVERVIEW
            // ============================================================
            var paidOrders = orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).ToList();

            decimal totalRevenue = paidOrders.Sum(o => o.Total);
            decimal platformFee = Math.Round(totalRevenue * 0.05m, 2); // 5% phí nền tảng
            decimal totalRefundAmount = refundRequests
                .Where(r => r.Status == RefundStatus.AdminApproved)
                .Sum(r => r.RefundAmount);
            decimal netRevenue = totalRevenue - totalRefundAmount;

            int totalOrders = orders.Count();
            int completedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
            int cancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);
            int pendingOrders = orders.Count(o =>
                o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing);

            decimal avgOrderValue = completedOrders > 0
                ? Math.Round(totalRevenue / completedOrders, 2)
                : 0;

            // Users statistics
            int totalUsers = allUsers.Count();

            // NEW users trong khoảng - User entity không có CreatedAt
            int newUsers = 0; // TODO: Implement nếu cần track user creation date

            // NEW shops trong khoảng - Shop có CreatedAt rồi! 
            int newShops = allShops.Count(s => s.CreatedAt >= fromDate && s.CreatedAt <= toDate);

            var overview = new AdminDashboardOverviewDTO
            {
                From = fromDate,
                To = toDate,
                TotalOrders = totalOrders,
                CompletedOrders = completedOrders,
                CancelledOrders = cancelledOrders,
                PendingOrders = pendingOrders,
                TotalRevenue = totalRevenue,
                PlatformFee = platformFee,
                TotalRefundAmount = totalRefundAmount,
                NetRevenue = netRevenue,
                AverageOrderValue = avgOrderValue,
                TotalUsers = totalUsers,
                NewUsers = newUsers,
                ActiveShops = allShops.Count(s => s.Status == "Approved"),
                NewShops = newShops
            };

            // ============================================================
            // REVENUE CHART (theo ngày)
            // ============================================================
            var revenueChart = paidOrders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new AdminRevenuePointDTO
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.Total),
                    PlatformFee = Math.Round(g.Sum(x => x.Total) * 0.05m, 2),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            // ============================================================
            // TOP SHOPS (theo doanh thu)
            // ============================================================
            var topShops = orders
                .Where(o => o.Shop != null && o.PaymentStatus == PaymentStatus.Paid)
                .GroupBy(o => o.ShopId)
                .Select(g =>
                {
                    var firstOrder = g.First();
                    var shop = firstOrder.Shop;

                    // Lấy feedbacks của shop
                    var shopFeedbacks = allFeedbacks.Where(f => f.ShopId == g.Key).ToList();
                    double avgRating = shopFeedbacks.Any()
                        ? Math.Round(shopFeedbacks.Average(f => f.Rating), 2)
                        : 0;

                    // Đếm số sản phẩm của shop
                    int productCount = allProducts.Count(p => p.ShopId == g.Key);

                    return new AdminTopShopDTO
                    {
                        ShopId = shop.Id,
                        ShopName = shop.Name,
                        SellerName = shop.Seller?.FullName ?? shop.Seller?.UserName ?? "Unknown",
                        ShopLogoUrl = shop.ShopAvatar,
                        TotalRevenue = g.Sum(o => o.Total),
                        TotalOrders = g.Count(),
                        AverageRating = avgRating,
                        ProductCount = productCount
                    };
                })
                .OrderByDescending(s => s.TotalRevenue)
                .Take(10)
                .ToList();

            // ============================================================
            // TOP PRODUCTS (theo doanh thu toàn hệ thống)
            // ============================================================
            var allDetails = orders
                .SelectMany(o => o.Details)
                .Where(d => d.Product != null)
                .ToList();

            var topProducts = allDetails
                .GroupBy(d => d.ProductId)
                .Select(g =>
                {
                    var first = g.First();
                    var product = first.Product;

                    // Lấy shop của product
                    var shop = allShops.FirstOrDefault(s => s.Id == product.ShopId);

                    // Lấy feedbacks của product
                    var productFeedbacks = allFeedbacks.Where(f => f.ProductId == g.Key).ToList();
                    double avgRating = productFeedbacks.Any()
                        ? Math.Round(productFeedbacks.Average(f => f.Rating), 2)
                        : 0;

                    // Ảnh: ưu tiên IsPrimary
                    string thumbnail = null;
                    if (product.Images != null && product.Images.Any())
                    {
                        var primary = product.Images.FirstOrDefault(i => i.IsPrimary);
                        thumbnail = primary?.Url ?? product.Images.First().Url;
                    }

                    return new AdminTopProductDTO
                    {
                        ProductId = g.Key,
                        ProductName = product.Name,
                        ThumbnailUrl = thumbnail,
                        ShopName = shop?.Name ?? "Unknown",
                        ShopId = product.ShopId,
                        TotalQuantity = g.Sum(x => x.Quantity),
                        TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice),
                        AverageRating = avgRating,
                        FeedbackCount = productFeedbacks.Count
                    };
                })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(10)
                .ToList();

            // ============================================================
            // RECENT ORDERS
            // ============================================================
            var recentOrders = orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(o => new AdminOrderMiniDTO
                {
                    OrderId = o.Id,
                    OrderCode = o.OrderCode,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status.ToString(),
                    PaymentStatus = o.PaymentStatus.ToString(),
                    Total = o.Total,
                    CustomerName = o.User?.FullName ?? o.User?.UserName ?? "Unknown",
                    ShopName = o.Shop?.Name ?? "Unknown"
                })
                .ToList();

            // ============================================================
            // REFUND SUMMARY
            // ============================================================
            var refundSummary = new AdminRefundSummaryDTO
            {
                TotalRequests = refundRequests.Count(),
                PendingAdminCount = refundRequests.Count(r => r.Status == RefundStatus.PendingAdmin),
                ApprovedCount = refundRequests.Count(r => r.Status == RefundStatus.AdminApproved),
                RejectedCount = refundRequests.Count(r => r.Status == RefundStatus.ShopRejected),
                TotalRefundAmount = totalRefundAmount
            };

            // ============================================================
            // USER SUMMARY
            // ============================================================
            var userSummary = await BuildUserSummaryAsync(allUsers, orders);

            // ============================================================
            // SYSTEM HEALTH
            // ============================================================
            var systemHealth = BuildSystemHealth(allProducts, allShops, allFeedbacks);

            // ============================================================
            // FINAL DTO
            // ============================================================
            var result = new AdminDashboardDTO
            {
                Range = new AdminDashboardRangeDTO
                {
                    From = fromDate,
                    To = toDate
                },
                Overview = overview,
                RevenueChart = revenueChart,
                TopShops = topShops,
                TopProducts = topProducts,
                RecentOrders = recentOrders,
                RefundSummary = refundSummary,
                UserSummary = userSummary,
                SystemHealth = systemHealth
            };

            return result;
        }

        private async Task<AdminUserSummaryDTO> BuildUserSummaryAsync(
            IEnumerable<Data.Entities.User> allUsers,
            IEnumerable<Data.Entities.Order> orders)
        {
            var userList = allUsers.ToList();

            // Đếm users theo role bằng UserManager
            int buyerCount = 0;
            int sellerCount = 0;
            int adminCount = 0;

            foreach (var user in userList)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Case-insensitive comparison
                if (roles.Any(r => r.Equals("Buyer", StringComparison.OrdinalIgnoreCase)))
                    buyerCount++;
                if (roles.Any(r => r.Equals("Seller", StringComparison.OrdinalIgnoreCase)))
                    sellerCount++;
                if (roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
                    adminCount++;
            }

            // Nếu buyerCount = 0, có thể users không có role Buyer explicit
            // Tính buyers = totalUsers - sellers - admins
            if (buyerCount == 0)
            {
                buyerCount = userList.Count - sellerCount - adminCount;
            }

            // Active users = users có đơn hàng trong khoảng
            var activeUserIds = orders.Select(o => o.UserId).Distinct();
            int activeUsers = activeUserIds.Count();

            return new AdminUserSummaryDTO
            {
                TotalUsers = userList.Count,
                BuyerCount = buyerCount,
                SellerCount = sellerCount,
                AdminCount = adminCount,
                NewUsersInRange = 0, // TODO: Implement nếu User có CreatedAt
                ActiveUsersInRange = activeUsers
            };
        }

        private AdminSystemHealthDTO BuildSystemHealth(
            IEnumerable<Data.Entities.Product> products,
            IEnumerable<Data.Entities.Shop> shops,
            IEnumerable<Data.Entities.Feedback> feedbacks)
        {
            var productList = products.ToList();
            var shopList = shops.ToList();
            var feedbackList = feedbacks.ToList();

            int totalProducts = productList.Count;
            int activeProducts = productList.Count(p => p.Active == 1);
            int outOfStockProducts = productList.Count(p => p.Stock == 0);

            int totalShops = shopList.Count;
            int activeShops = shopList.Count(s => s.Status == "Approved");
            int pendingShops = shopList.Count(s => s.Status == "Pending");

            double avgSystemRating = feedbackList.Any()
                ? Math.Round(feedbackList.Average(f => f.Rating), 2)
                : 0;

            return new AdminSystemHealthDTO
            {
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                OutOfStockProducts = outOfStockProducts,
                TotalShops = totalShops,
                ActiveShops = activeShops,
                PendingShops = pendingShops,
                AverageSystemRating = avgSystemRating,
                TotalFeedbacks = feedbackList.Count
            };
        }
    }
}