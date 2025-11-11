using LECOMS.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    /// <summary>
    /// Service interface cho Payment với PayOS
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Tạo payment link cho 1 order đơn lẻ
        /// Use case: Retry payment, manual payment link generation
        /// </summary>
        Task<string> CreatePaymentLinkAsync(string orderId);

        /// <summary>
        /// ⭐ NEW: Tạo payment link cho NHIỀU orders (sàn thu hộ)
        /// Use case: Normal checkout flow với nhiều shops
        /// </summary>
        /// <param name="transactionId">ID của transaction đã tạo</param>
        /// <param name="orders">Danh sách orders cần thanh toán</param>
        /// <returns>Payment URL từ PayOS</returns>
        Task<string> CreatePaymentLinkForMultipleOrdersAsync(string transactionId, List<Order> orders);

        /// <summary>
        /// Xử lý webhook callback từ PayOS
        /// </summary>
        Task<bool> HandlePayOSWebhookAsync(string webhookData);

        /// <summary>
        /// Verify PayOS webhook signature
        /// </summary>
        Task<bool> VerifyPayOSSignatureAsync(string webhookData, string signature);

        /// <summary>
        /// Lấy transaction status
        /// </summary>
        Task<Transaction?> GetTransactionStatusAsync(string orderId);

        /// <summary>
        /// Cancel payment
        /// </summary>
        Task<bool> CancelPaymentAsync(string orderId);
    }
}