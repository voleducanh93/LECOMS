using LECOMS.Data.DTOs.Refund;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IRefundService
    {
        Task<RefundRequestDTO> CreateAsync(string customerId, CreateRefundRequestDTO dto);

        Task<IEnumerable<RefundRequestDTO>> GetMyAsync(string customerId);

        Task<IEnumerable<RefundRequestDTO>> GetForShopAsync(string sellerId);

        Task<IEnumerable<RefundRequestDTO>> GetPendingAdminAsync();

        Task<RefundRequestDTO> SellerDecisionAsync(string refundId, string sellerId, bool approve, string? rejectReason);

        Task<RefundRequestDTO> AdminDecisionAsync(string refundId, string adminId, bool approve, string? rejectReason);

        // ✅ Customer bổ sung / cập nhật evidence cho Refund
        Task<RefundRequestDTO> AddEvidenceAsync(string refundId, string customerId, string[] urls);

        // ✅ Admin dashboard thống kê refund
        Task<RefundDashboardDTO> GetAdminDashboardAsync();
    }
}
