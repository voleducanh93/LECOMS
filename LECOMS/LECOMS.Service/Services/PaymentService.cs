using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;

        public PaymentService(IUnitOfWork uow, IConfiguration config)
        {
            _uow = uow;
            _config = config;
        }

        public Task<string> CreateVietQrPaymentAsync(string paymentId, decimal amount)
        {
            // Stub: generate a fake VietQr URL — replace with real VietQR API call later.
            // Use a friendly URL that includes paymentId so webhook/test flows can correlate.
            var baseUrl = _config["AppUrl"] ?? "https://localhost:5001";
            var qrUrl = $"{baseUrl}/pay/vietqr?paymentId={Uri.EscapeDataString(paymentId)}&amount={amount:F2}";
            return Task.FromResult(qrUrl);
        }

        public async Task<bool> HandleVietQrWebhookAsync(string payload)
        {
            // Very small stub: expect payload contains paymentId and status=success|failed
            // Example payload: "paymentId=...&status=success"
            try
            {
                var kv = System.Web.HttpUtility.ParseQueryString(payload);
                var paymentId = kv["paymentId"];
                var status = kv["status"];

                if (string.IsNullOrEmpty(paymentId) || string.IsNullOrEmpty(status))
                    return false;

                var payment = await _uow.Payments.GetAsync(p => p.Id == paymentId);
                if (payment == null) return false;

                if (status.Equals("success", StringComparison.OrdinalIgnoreCase))
                {
                    payment.Status = Data.Enum.PaymentStatus.Succeeded;
                    // mark order as paid
                    var order = await _uow.Orders.GetAsync(o => o.Id == payment.OrderId);
                    if (order != null) order.Status = Data.Enum.OrderStatus.Paid;
                }
                else
                {
                    payment.Status = Data.Enum.PaymentStatus.Failed;
                }

                await _uow.Payments.UpdateAsync(payment);
                await _uow.CompleteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}