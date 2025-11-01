using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IPaymentService
    {
        /// <summary>
        /// Create a VietQr payment URL (stub). Returns a url the frontend can show (QR or redirect).
        /// </summary>
        Task<string> CreateVietQrPaymentAsync(string paymentId, decimal amount);
        /// <summary>
        /// Handle provider webhook payload. Returns true if handled.
        /// </summary>
        Task<bool> HandleVietQrWebhookAsync(string payload);
    }
}
