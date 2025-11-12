using LECOMS.Data.Entities;
using LECOMS.Data.DTOs.Refund;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    /// <summary>
    /// Service xử lý Customer Refund
    /// Shop-decides model: Shop approve/reject, market self-regulates
    /// </summary>
    public interface IRefundService
    {
        // Customer methods
        Task<RefundRequest> CreateRefundRequestAsync(CreateRefundRequestDto dto);
        Task<RefundRequest> CancelRefundRequestAsync(string refundId, string customerId);

        // Shop methods
        Task<RefundRequest> ShopApproveRefundAsync(string refundId, string shopUserId);
        Task<RefundRequest> ShopRejectRefundAsync(string refundId, string shopUserId, string reason);

        // Query methods
        Task<RefundRequest?> GetRefundRequestAsync(string refundId);
        Task<IEnumerable<RefundRequest>> GetRefundRequestsByUserAsync(string userId, int pageNumber = 1, int pageSize = 20);
        Task<IEnumerable<RefundRequest>> GetRefundRequestsByOrderAsync(string orderId);
        Task<IEnumerable<RefundRequest>> GetShopPendingRefundsAsync(int shopId);
        Task<IEnumerable<RefundRequest>> GetShopRefundHistoryAsync(int shopId, int pageNumber = 1, int pageSize = 20);

        // Statistics
        Task<RefundStatistics> GetShopRefundStatisticsAsync(int shopId);
        Task<CustomerRefundStatistics> GetCustomerRefundStatisticsAsync(string customerId);
    }

    public class RefundStatistics
    {
        public int TotalRequests { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public decimal ApprovalRate { get; set; }
        public decimal ResponseRate { get; set; }
        public double AverageResponseTimeHours { get; set; }
    }

    public class CustomerRefundStatistics
    {
        public int TotalOrders { get; set; }
        public int TotalRefunds { get; set; }
        public decimal RefundRate { get; set; }
        public bool IsHighRisk { get; set; }
    }
}