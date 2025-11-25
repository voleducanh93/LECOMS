using LECOMS.Data.DTOs.Feedback;
using LECOMS.Data.Entities;
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
    public class FeedbackService : IFeedbackService
    {
        private readonly IUnitOfWork _uow;

        public FeedbackService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<FeedbackDTO> CreateFeedbackAsync(string userId, CreateFeedbackRequestDTO dto)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
                throw new InvalidOperationException("Đánh giá phải nằm trong khoảng từ 1 đến 5.");

            // Lấy order + details để verify
            var order = await _uow.Orders.GetAsync(
                o => o.Id == dto.OrderId &&
                     o.UserId == userId &&
                     o.PaymentStatus == PaymentStatus.Paid &&
                     o.Status == OrderStatus.Completed,
                includeProperties: "Details,User"
            );

            if (order == null)
                throw new InvalidOperationException("Đơn đặt hàng không đủ điều kiện để phản hồi.");

            var orderedProduct = order.Details.FirstOrDefault(d => d.ProductId == dto.ProductId);
            if (orderedProduct == null)
                throw new InvalidOperationException("Không tìm thấy sản phẩm theo thứ tự này.");

            // Check đã feedback rồi (1 feedback / order / product)
            var existing = await _uow.Feedbacks.GetAsync(f =>
                f.OrderId == dto.OrderId &&
                f.ProductId == dto.ProductId &&
                f.UserId == userId
            );

            if (existing != null)
                throw new InvalidOperationException("Bạn đã đưa ra phản hồi cho sản phẩm này theo thứ tự này.");

            var feedback = new Feedback
            {
                OrderId = dto.OrderId,
                UserId = userId,
                ProductId = dto.ProductId,
                ShopId = order.ShopId,
                Rating = dto.Rating,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Feedbacks.AddAsync(feedback);

            // Thêm hình ảnh nếu có
            if (dto.ImageUrls != null && dto.ImageUrls.Any())
            {
                foreach (var url in dto.ImageUrls)
                {
                    var img = new FeedbackImage
                    {
                        FeedbackId = feedback.Id,
                        Url = url
                    };
                    await _uow.FeedbackImages.AddAsync(img);
                    feedback.Images.Add(img);
                }
            }

            await _uow.CompleteAsync();

            // Load lại để map DTO (user, images, reply)
            var fullFeedback = await _uow.Feedbacks.GetAsync(
                f => f.Id == feedback.Id,
                includeProperties: "User,Images,Reply"
            );

            return MapToDTO(fullFeedback);
        }

        public async Task<IEnumerable<FeedbackDTO>> GetProductFeedbacksAsync(string productId, int? rating = null)
        {
            var list = await _uow.Feedbacks.GetAllAsync(
                f => f.ProductId == productId,
                includeProperties: "User,Images,Reply"
            );

            if (rating.HasValue)
                list = list.Where(f => f.Rating == rating.Value);

            return list
                .OrderByDescending(f => f.CreatedAt)
                .Select(MapToDTO)
                .ToList();
        }

        public async Task<IEnumerable<FeedbackDTO>> GetShopFeedbacksAsync(int shopId, int? rating = null)
        {
            var list = await _uow.Feedbacks.GetAllAsync(
                f => f.ShopId == shopId,
                includeProperties: "User,Images,Reply"
            );

            if (rating.HasValue)
                list = list.Where(f => f.Rating == rating.Value);

            return list
                .OrderByDescending(f => f.CreatedAt)
                .Select(MapToDTO)
                .ToList();
        }

        public async Task<FeedbackDTO> ReplyFeedbackAsync(string sellerUserId, string feedbackId, ReplyFeedbackRequestDTO dto)
        {
            // Lấy shop của seller
            var shop = await _uow.Shops.GetAsync(s => s.SellerId == sellerUserId);
            if (shop == null)
                throw new InvalidOperationException("Người bán không có cửa hàng.");

            var feedback = await _uow.Feedbacks.GetAsync(
                f => f.Id == feedbackId,
                includeProperties: "Reply,User,Images"
            );

            if (feedback == null)
                throw new InvalidOperationException("Không tìm thấy phản hồi.");

            if (feedback.ShopId != shop.Id)
                throw new InvalidOperationException("Bạn không thể trả lời phản hồi của cửa hàng khác.");

            if (feedback.Reply != null)
                throw new InvalidOperationException("Phản hồi đã có phản hồi rồi.");

            var reply = new FeedbackReply
            {
                FeedbackId = feedbackId,
                ShopId = shop.Id,
                ReplyContent = dto.ReplyContent,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.FeedbackReplies.AddAsync(reply);
            await _uow.CompleteAsync();

            // Load lại để map
            var fullFeedback = await _uow.Feedbacks.GetAsync(
                f => f.Id == feedbackId,
                includeProperties: "User,Images,Reply"
            );

            return MapToDTO(fullFeedback);
        }

        private FeedbackDTO MapToDTO(Feedback f)
        {
            if (f == null) return null;

            return new FeedbackDTO
            {
                Id = f.Id,
                UserId = f.UserId,
                UserName = f.User?.FullName ?? f.User?.UserName ?? "Unknown",
                UserAvatar = f.User?.ImageUrl,
                ProductId = f.ProductId,
                ShopId = f.ShopId,
                Rating = f.Rating,
                Content = f.Content,
                CreatedAt = f.CreatedAt,
                Images = f.Images?.Select(i => i.Url).ToList() ?? new List<string>(),
                Reply = f.Reply != null ? new
                {
                    content = f.Reply.ReplyContent,
                    createdAt = f.Reply.CreatedAt
                } : null
            };
        }

        public async Task<IEnumerable<FeedbackDTO>> GetShopFeedbacksForSellerAsync(string sellerUserId, int? rating = null)
        {
            var shop = await _uow.Shops.GetAsync(s => s.SellerId == sellerUserId);

            if (shop == null)
                throw new InvalidOperationException("Người bán không có cửa hàng.");

            var list = await _uow.Feedbacks.GetAllAsync(
                f => f.ShopId == shop.Id,
                includeProperties: "User,Images,Reply"
            );

            if (rating.HasValue)
                list = list.Where(f => f.Rating == rating.Value);

            return list
                .OrderByDescending(f => f.CreatedAt)
                .Select(MapToDTO)
                .ToList();
        }

        public async Task<ProductFeedbackSummaryDTO> GetProductFeedbackSummaryAsync(string productId)
        {
            var list = await _uow.Feedbacks.GetAllAsync(
                f => f.ProductId == productId,
                includeProperties: "User,Images,Reply"
            );

            if (!list.Any())
            {
                return new ProductFeedbackSummaryDTO
                {
                    ProductId = productId,
                    AverageRating = 0,
                    TotalFeedbackCount = 0,
                    Rating1Count = 0,
                    Rating2Count = 0,
                    Rating3Count = 0,
                    Rating4Count = 0,
                    Rating5Count = 0,
                    PositiveRate = 0,
                    RecentFeedbacks = new List<FeedbackDTO>()
                };
            }

            var summary = new ProductFeedbackSummaryDTO
            {
                ProductId = productId,
                TotalFeedbackCount = list.Count(),
                AverageRating = Math.Round(list.Average(f => f.Rating), 2),

                Rating1Count = list.Count(f => f.Rating == 1),
                Rating2Count = list.Count(f => f.Rating == 2),
                Rating3Count = list.Count(f => f.Rating == 3),
                Rating4Count = list.Count(f => f.Rating == 4),
                Rating5Count = list.Count(f => f.Rating == 5),

                PositiveRate = Math.Round(
                    (double)list.Count(f => f.Rating >= 4) / list.Count() * 100, 2),

                RecentFeedbacks = list
                    .OrderByDescending(f => f.CreatedAt)
                    .Take(5)
                    .Select(MapToDTO)
                    .ToList()
            };

            return summary;
        }

        public async Task<ShopFeedbackReportDTO> GetShopFeedbackReportAsync(string sellerUserId)
        {
            var shop = await _uow.Shops.GetAsync(s => s.SellerId == sellerUserId);
            if (shop == null)
                throw new InvalidOperationException("Người bán không có cửa hàng.");

            var list = await _uow.Feedbacks.GetAllAsync(
                f => f.ShopId == shop.Id,
                includeProperties: "User,Images,Reply,Product"
            );

            var groupedByProduct = list
                .GroupBy(x => x.ProductId)
                .Select(g => new LowRatingProductDTO
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product.Name,
                    AvgRating = Math.Round(g.Average(x => x.Rating), 2),
                    FeedbackCount = g.Count()
                })
                .Where(x => x.AvgRating <= 3.0)
                .OrderBy(x => x.AvgRating)
                .ToList();

            return new ShopFeedbackReportDTO
            {
                ShopId = shop.Id,
                ShopName = shop.Name,
                AverageRating = list.Any() ? Math.Round(list.Average(f => f.Rating), 2) : 0,
                TotalFeedback = list.Count(),

                RatingCounts = new Dictionary<int, int>
        {
            {1, list.Count(f => f.Rating == 1)},
            {2, list.Count(f => f.Rating == 2)},
            {3, list.Count(f => f.Rating == 3)},
            {4, list.Count(f => f.Rating == 4)},
            {5, list.Count(f => f.Rating == 5)},
        },

                RecentFeedbacks = list
                    .OrderByDescending(f => f.CreatedAt)
                    .Take(10)
                    .Select(MapToDTO),

                LowRatingProducts = groupedByProduct
            };
        }

        public async Task<AdminFeedbackDashboardDTO> GetAdminFeedbackDashboardAsync()
        {
            var feedbacks = await _uow.Feedbacks.GetAllAsync(
                filter: null,
                includeProperties: "User,Images,Reply,Product,Product.Shop"
            );

            if (!feedbacks.Any())
            {
                return new AdminFeedbackDashboardDTO
                {
                    TotalFeedback = 0,
                    AverageRating = 0,
                    PositiveRate = 0,
                    NegativeRate = 0,
                    RecentFeedbacks = new List<FeedbackDTO>(),
                    BestRatedProducts = new List<TopRatedProductDTO>(),
                    WorstRatedProducts = new List<TopRatedProductDTO>(),
                    SellerWithWorstRating = new List<SellerFeedbackStatsDTO>()
                };
            }

            var totalFeedback = feedbacks.Count();
            var averageRating = Math.Round(feedbacks.Average(f => f.Rating), 2);

            var positiveCount = feedbacks.Count(f => f.Rating >= 4);
            var negativeCount = feedbacks.Count(f => f.Rating <= 2);

            var groupedByProduct = feedbacks
                .GroupBy(f => f.ProductId)
                .Select(g => new TopRatedProductDTO
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product.Name,
                    AverageRating = Math.Round(g.Average(x => x.Rating), 2),
                    FeedbackCount = g.Count()
                }).ToList();

            var bestRatedProducts = groupedByProduct
                .Where(x => x.FeedbackCount >= 3)
                .OrderByDescending(x => x.AverageRating)
                .Take(10)
                .ToList();

            var worstRatedProducts = groupedByProduct
                .Where(x => x.FeedbackCount >= 3)
                .OrderBy(x => x.AverageRating)
                .Take(10)
                .ToList();

            var sellerStats = feedbacks
                .GroupBy(f => f.ShopId)
                .Select(g => new SellerFeedbackStatsDTO
                {
                    ShopId = g.Key,
                    ShopName = g.First().Product.Shop.Name,
                    AverageRating = Math.Round(g.Average(x => x.Rating), 2),
                    TotalFeedback = g.Count(),
                    LowRatingCount = g.Count(x => x.Rating <= 2)
                })
                .OrderByDescending(x => x.LowRatingCount)
                .Take(10)
                .ToList();

            return new AdminFeedbackDashboardDTO
            {
                TotalFeedback = totalFeedback,
                AverageRating = averageRating,
                PositiveRate = Math.Round((double)positiveCount / totalFeedback * 100, 2),
                NegativeRate = Math.Round((double)negativeCount / totalFeedback * 100, 2),

                RecentFeedbacks = feedbacks
                    .OrderByDescending(f => f.CreatedAt)
                    .Take(10)
                    .Select(MapToDTO)
                    .ToList(),

                RecentLowRatings = feedbacks
                    .Where(f => f.Rating <= 2)
                    .OrderByDescending(f => f.CreatedAt)
                    .Take(10)
                    .Select(MapToDTO)
                    .ToList(),

                BestRatedProducts = bestRatedProducts,
                WorstRatedProducts = worstRatedProducts,
                SellerWithWorstRating = sellerStats
            };
        }

        // ================================
        // 2) Customer/Seller update feedback của mình
        // ================================
        public async Task<FeedbackDTO> UpdateFeedbackAsync(string userId, string feedbackId, UpdateFeedbackRequestDTO dto)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
                throw new InvalidOperationException("Đánh giá phải nằm trong khoảng từ 1 đến 5.");

            var feedback = await _uow.Feedbacks.GetAsync(
                f => f.Id == feedbackId,
                includeProperties: "User,Images,Reply"
            );

            if (feedback == null)
                throw new InvalidOperationException("Không tìm thấy phản hồi.");

            if (feedback.UserId != userId)
                throw new InvalidOperationException("Bạn chỉ có thể cập nhật phản hồi của riêng bạn.");

            // Update core fields
            feedback.Rating = dto.Rating;
            feedback.Content = dto.Content;

            // Xử lý ảnh:
            // - Nếu ImageUrls = null => giữ nguyên
            // - Nếu ImageUrls != null => replace toàn bộ
            if (dto.ImageUrls != null)
            {
                // Xóa ảnh cũ
                if (feedback.Images != null && feedback.Images.Any())
                {
                    foreach (var img in feedback.Images.ToList())
                    {
                        await _uow.FeedbackImages.DeleteAsync(img);
                    }
                    feedback.Images.Clear();
                }

                // Thêm ảnh mới
                foreach (var url in dto.ImageUrls)
                {
                    var img = new FeedbackImage
                    {
                        FeedbackId = feedback.Id,
                        Url = url
                    };
                    await _uow.FeedbackImages.AddAsync(img);
                    feedback.Images.Add(img);
                }
            }

            await _uow.Feedbacks.UpdateAsync(feedback);
            await _uow.CompleteAsync();

            // Load lại cho chắc navigation
            var full = await _uow.Feedbacks.GetAsync(
                f => f.Id == feedbackId,
                includeProperties: "User,Images,Reply"
            );

            return MapToDTO(full);
        }

        // ================================
        // 3) Owner delete feedback của mình
        // ================================
        public async Task<bool> DeleteFeedbackByOwnerAsync(string userId, string feedbackId)
        {
            var feedback = await _uow.Feedbacks.GetAsync(
                f => f.Id == feedbackId,
                includeProperties: "Images,Reply"
            );

            if (feedback == null)
                throw new InvalidOperationException("Không tìm thấy phản hồi.");

            if (feedback.UserId != userId)
                throw new InvalidOperationException("Bạn chỉ có thể xóa phản hồi của riêng bạn.");

            // Xóa reply nếu có
            if (feedback.Reply != null)
            {
                await _uow.FeedbackReplies.DeleteAsync(feedback.Reply);
            }

            // Xóa images
            if (feedback.Images != null && feedback.Images.Any())
            {
                foreach (var img in feedback.Images.ToList())
                    await _uow.FeedbackImages.DeleteAsync(img);
            }

            await _uow.Feedbacks.DeleteAsync(feedback);
            await _uow.CompleteAsync();

            return true;
        }

        // ================================
        // 4) Admin delete bất kỳ feedback
        // ================================
        public async Task<bool> DeleteFeedbackByAdminAsync(string adminUserId, string feedbackId)
        {
            // Nếu muốn log lại adminUserId, bạn có thể lưu log / audit ở đây
            var feedback = await _uow.Feedbacks.GetAsync(
                f => f.Id == feedbackId,
                includeProperties: "Images,Reply"
            );

            if (feedback == null)
                throw new InvalidOperationException("Không tìm thấy phản hồi.");

            if (feedback.Reply != null)
                await _uow.FeedbackReplies.DeleteAsync(feedback.Reply);

            if (feedback.Images != null && feedback.Images.Any())
            {
                foreach (var img in feedback.Images.ToList())
                    await _uow.FeedbackImages.DeleteAsync(img);
            }

            await _uow.Feedbacks.DeleteAsync(feedback);
            await _uow.CompleteAsync();

            return true;
        }

        // ================================
        // 5) Seller update reply
        // ================================
        public async Task<FeedbackDTO> UpdateFeedbackReplyAsync(
            string sellerUserId,
            string feedbackId,
            UpdateReplyFeedbackRequestDTO dto)
        {
            var shop = await _uow.Shops.GetAsync(s => s.SellerId == sellerUserId);
            if (shop == null)
                throw new InvalidOperationException("Người bán không có cửa hàng.");

            var feedback = await _uow.Feedbacks.GetAsync(
                f => f.Id == feedbackId,
                includeProperties: "Reply,User,Images"
            );

            if (feedback == null)
                throw new InvalidOperationException("Không tìm thấy phản hồi.");

            if (feedback.ShopId != shop.Id)
                throw new InvalidOperationException("Bạn không thể sửa đổi câu trả lời của cửa hàng khác.");

            if (feedback.Reply == null)
                throw new InvalidOperationException("Phản hồi chưa có câu trả lời.");

            feedback.Reply.ReplyContent = dto.ReplyContent;
            // Nếu model của bạn có UpdatedAt thì set ở đây, nếu không thì thôi

            await _uow.FeedbackReplies.UpdateAsync(feedback.Reply);
            await _uow.CompleteAsync();

            var full = await _uow.Feedbacks.GetAsync(
                f => f.Id == feedbackId,
                includeProperties: "User,Images,Reply"
            );

            return MapToDTO(full);
        }

        // ================================
        // 6) Seller xóa reply
        // ================================
        public async Task<FeedbackDTO> DeleteFeedbackReplyAsync(string sellerUserId, string feedbackId)
        {
            var shop = await _uow.Shops.GetAsync(s => s.SellerId == sellerUserId);
            if (shop == null)
                throw new InvalidOperationException("Người bán không có cửa hàng.");

            var feedback = await _uow.Feedbacks.GetAsync(
                f => f.Id == feedbackId,
                includeProperties: "Reply,User,Images"
            );

            if (feedback == null)
                throw new InvalidOperationException("Không tìm thấy phản hồi.");

            if (feedback.ShopId != shop.Id)
                throw new InvalidOperationException("Bạn không thể xóa trả lời của cửa hàng khác.");

            if (feedback.Reply == null)
                throw new InvalidOperationException("Phản hồi không có câu trả lời.");

            await _uow.FeedbackReplies.DeleteAsync(feedback.Reply);
            feedback.Reply = null;

            await _uow.Feedbacks.UpdateAsync(feedback);
            await _uow.CompleteAsync();

            var full = await _uow.Feedbacks.GetAsync(
                f => f.Id == feedbackId,
                includeProperties: "User,Images,Reply"
            );

            return MapToDTO(full);
        }

    }
}
