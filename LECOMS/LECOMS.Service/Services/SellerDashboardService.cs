using LECOMS.Data.DTOs.Seller;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class SellerDashboardService : ISellerDashboardService
    {
        private readonly IUnitOfWork _uow;

        public SellerDashboardService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<SellerDashboardDTO> GetSellerDashboardAsync(
            string sellerUserId,
            DateTime from,
            DateTime to)
        {
            // 1. Lấy shop theo seller
            var shop = await _uow.Shops.GetAsync(s => s.SellerId == sellerUserId);
            if (shop == null)
                throw new InvalidOperationException("Người bán không có cửa hàng.");

            int shopId = shop.Id;

            var fromDate = from;
            var toDate = to;

            // 2. Orders trong khoảng
            var orders = await _uow.Orders.GetAllAsync(
                o => o.ShopId == shopId &&
                     o.CreatedAt >= fromDate &&
                     o.CreatedAt <= toDate,
                includeProperties: "User,Details,Details.Product,Details.Product.Images"
            );

            // 3. Refund requests
            var refundRequests = await _uow.RefundRequests.GetAllAsync(
                r => r.Order.ShopId == shopId &&
                     r.RequestedAt >= fromDate &&
                     r.RequestedAt <= toDate,
                includeProperties: "Order"
            );

            // 4. Feedback
            var feedbacks = await _uow.Feedbacks.GetAllAsync(
                f => f.ShopId == shopId &&
                     f.CreatedAt >= fromDate &&
                     f.CreatedAt <= toDate
            );

            // 5. Ví shop
            var shopWallet = await _uow.ShopWallets.GetAsync(w => w.ShopId == shopId);

            var withdrawalRequests = await _uow.WithdrawalRequests.GetAllAsync(
                w => w.ShopId == shopId &&
                w.RequestedAt >= fromDate &&
                w.RequestedAt <= toDate
);

            // ============================================================
            // OVERVIEW
            // ============================================================
            var paidOrders = orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).ToList();

            decimal totalRevenue = paidOrders.Sum(o => o.Total);
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

            int uniqueCustomers = orders
                .Select(o => o.UserId)
                .Distinct()
                .Count();

            var overview = new SellerDashboardOverviewDTO
            {
                From = fromDate,
                To = toDate,
                TotalOrders = totalOrders,
                CompletedOrders = completedOrders,
                CancelledOrders = cancelledOrders,
                PendingOrders = pendingOrders,
                TotalRevenue = totalRevenue,
                TotalRefundAmount = totalRefundAmount,
                NetRevenue = netRevenue,
                AverageOrderValue = avgOrderValue,
                UniqueCustomers = uniqueCustomers
            };

            // ============================================================
            // REVENUE CHART (theo ngày)
            // ============================================================
            var revenueChart = paidOrders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new SellerRevenuePointDTO
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.Total)
                })
                .OrderBy(x => x.Date)
                .ToList();

            // ============================================================
            // TOP PRODUCTS
            // ============================================================
            var allDetails = orders
                .SelectMany(o => o.Details)
                .Where(d => d.Product != null)
                .ToList();

            var productStats = allDetails
                .GroupBy(d => d.ProductId)
                .Select(g =>
                {
                    var first = g.First();
                    var product = first.Product;

                    var fbList = feedbacks.Where(f => f.ProductId == g.Key).ToList();
                    double avgRating = fbList.Any()
                        ? Math.Round(fbList.Average(f => f.Rating), 2)
                        : 0;

                    // Ảnh: ưu tiên IsPrimary, nếu không có thì lấy ảnh đầu tiên
                    string thumbnail = null;
                    if (product.Images != null && product.Images.Any())
                    {
                        var primary = product.Images.FirstOrDefault(i => i.IsPrimary);
                        thumbnail = primary?.Url ?? product.Images.First().Url;
                    }

                    return new SellerProductStatDTO
                    {
                        ProductId = g.Key,
                        ProductName = product.Name,
                        ThumbnailUrl = thumbnail,
                        TotalQuantity = g.Sum(x => x.Quantity),
                        TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice),
                        AverageRating = avgRating,
                        FeedbackCount = fbList.Count
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
                .Select(o => new SellerOrderMiniDTO
                {
                    OrderId = o.Id,
                    OrderCode = o.OrderCode,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status.ToString(),
                    PaymentStatus = o.PaymentStatus.ToString(),
                    Total = o.Total,
                    CustomerName = o.User?.FullName ?? o.User?.UserName ?? "Unknown"
                })
                .ToList();

            // ============================================================
            // REFUND SUMMARY
            // ============================================================
            var refundSummary = new SellerRefundSummaryDTO
            {
                TotalRequests = refundRequests.Count(),
                PendingCount = refundRequests.Count(r =>
                    r.Status == RefundStatus.PendingShop ||
                    r.Status == RefundStatus.PendingAdmin),
                ApprovedCount = refundRequests.Count(r => r.Status == RefundStatus.AdminApproved),
                RejectedCount = refundRequests.Count(r => r.Status == RefundStatus.ShopRejected),
                TotalRefundAmount = totalRefundAmount
            };

            // ============================================================
            // RATING SUMMARY
            // ============================================================
            var ratingSummary = BuildRatingSummary(feedbacks);

            // ============================================================
            // WALLET SUMMARY
            // ============================================================
            var walletSummary = BuildWalletSummary(shopWallet, withdrawalRequests);

            // ============================================================
            // FINAL DTO
            // ============================================================
            var result = new SellerDashboardDTO
            {
                ShopId = shop.Id,
                ShopName = shop.Name,
                Range = new SellerDashboardRangeDTO
                {
                    // View & BaseDate sẽ được fill ở Controller (vì controller biết view/date)
                    From = fromDate,
                    To = toDate
                },
                Overview = overview,
                RevenueChart = revenueChart,
                TopProducts = productStats,
                RecentOrders = recentOrders,
                RefundSummary = refundSummary,
                RatingSummary = ratingSummary,
                WalletSummary = walletSummary
            };

            return result;
        }

        private SellerRatingSummaryDTO BuildRatingSummary(IEnumerable<Data.Entities.Feedback> feedbacks)
        {
            var list = feedbacks.ToList();

            if (!list.Any())
            {
                return new SellerRatingSummaryDTO
                {
                    AverageRating = 0,
                    TotalFeedbacks = 0,
                    Rating1Count = 0,
                    Rating2Count = 0,
                    Rating3Count = 0,
                    Rating4Count = 0,
                    Rating5Count = 0,
                    PositiveRate = 0
                };
            }

            int total = list.Count;
            int r1 = list.Count(f => f.Rating == 1);
            int r2 = list.Count(f => f.Rating == 2);
            int r3 = list.Count(f => f.Rating == 3);
            int r4 = list.Count(f => f.Rating == 4);
            int r5 = list.Count(f => f.Rating == 5);

            double avg = Math.Round(list.Average(f => f.Rating), 2);
            int positiveCount = r4 + r5;
            double positiveRate = Math.Round((double)positiveCount / total * 100, 2);

            return new SellerRatingSummaryDTO
            {
                AverageRating = avg,
                TotalFeedbacks = total,
                Rating1Count = r1,
                Rating2Count = r2,
                Rating3Count = r3,
                Rating4Count = r4,
                Rating5Count = r5,
                PositiveRate = positiveRate
            };
        }

        private SellerWalletSummaryDTO BuildWalletSummary(
            Data.Entities.ShopWallet wallet,
            IEnumerable<Data.Entities.WithdrawalRequest> withdrawals)
        {
            if (wallet == null)
            {
                return new SellerWalletSummaryDTO
                {
                    AvailableBalance = 0,
                    PendingBalance = 0,
                    PendingWithdrawalAmount = 0,
                    ApprovedWithdrawalAmount = 0,
                    LastUpdatedAt = null
                };
            }

            var list = withdrawals?.ToList() ?? new List<Data.Entities.WithdrawalRequest>();

            var pendingAmount = list
                .Where(w => w.Status == WithdrawalStatus.Pending)
                .Sum(w => w.Amount);

            var approvedAmount = list
                .Where(w => w.Status == WithdrawalStatus.Approved)
                .Sum(w => w.Amount);

            return new SellerWalletSummaryDTO
            {
                AvailableBalance = wallet.AvailableBalance,
                PendingBalance = wallet.PendingBalance,
                PendingWithdrawalAmount = pendingAmount,
                ApprovedWithdrawalAmount = approvedAmount,
                LastUpdatedAt = wallet.LastUpdated
            };
        }
    }
}
