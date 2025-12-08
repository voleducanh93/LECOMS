using LECOMS.Data.DTOs.Feedback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IFeedbackService
    {
        Task<FeedbackDTO> CreateFeedbackAsync(string userId, CreateFeedbackRequestDTO dto);
        Task<IEnumerable<FeedbackDTO>> GetProductFeedbacksAsync(string productId, int? rating = null);
        Task<IEnumerable<FeedbackDTO>> GetShopFeedbacksAsync(int shopId, int? rating = null);
        Task<IEnumerable<FeedbackDTO>> GetShopFeedbacksForSellerAsync(string sellerUserId, int? rating = null);
        Task<FeedbackDTO> ReplyFeedbackAsync(string sellerUserId, string feedbackId, ReplyFeedbackRequestDTO dto);
        Task<ProductFeedbackSummaryDTO> GetProductFeedbackSummaryAsync(string productId);
        Task<ShopFeedbackReportDTO> GetShopFeedbackReportAsync(string sellerUserId);
        Task<AdminFeedbackDashboardDTO> GetAdminFeedbackDashboardAsync();
        Task<FeedbackDTO> UpdateFeedbackAsync(string userId, string feedbackId, UpdateFeedbackRequestDTO dto);
        Task<bool> DeleteFeedbackByOwnerAsync(string userId, string feedbackId);
        Task<bool> DeleteFeedbackByAdminAsync(string adminUserId, string feedbackId);
        Task<FeedbackDTO> UpdateFeedbackReplyAsync(string sellerUserId, string feedbackId, UpdateReplyFeedbackRequestDTO dto);
        Task<FeedbackDTO> DeleteFeedbackReplyAsync(string sellerUserId, string feedbackId);
        Task<FeedbackDTO> CreateFeedbackByUrlsAsync(string userId, CreateFeedbackRequestDTOV2 dto);
        Task<FeedbackDTO> UpdateFeedbackByUrlsAsync(string userId, string feedbackId, UpdateFeedbackRequestDTOV2 dto);
        Task<bool> HasFeedbackAsync(string userId, string orderId, string productId);
        Task<IEnumerable<string>> GetFeedbackedProductIdsInOrderAsync(string userId, string orderId);
    }

}
