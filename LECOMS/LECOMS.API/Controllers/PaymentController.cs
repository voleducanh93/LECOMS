using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace LECOMS.API.Controllers
{
    /// <summary>
    /// Controller xử lý thanh toán PayOS
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo payment link cho order
        /// </summary>
        /// <param name="orderId">ID của order cần thanh toán</param>
        /// <returns>Payment URL để redirect customer</returns>
        [HttpPost("create-payment-link")]
        [Authorize] // Customer phải login
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentLinkRequest request)
        {
            try
            {
                var paymentUrl = await _paymentService.CreatePaymentLinkAsync(request.OrderId);

                return Ok(new
                {
                    success = true,
                    paymentUrl = paymentUrl,
                    message = "Payment link created successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation creating payment link");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment link for Order {OrderId}", request.OrderId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Webhook callback từ PayOS
        /// URL: https://yourdomain.com/api/payment/payos-webhook
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous] // PayOS call không cần auth
        public async Task<IActionResult> PayOSWebhook()
        {
            try
            {
                // Read raw body
                using var reader = new System.IO.StreamReader(Request.Body);
                var webhookData = await reader.ReadToEndAsync();

                _logger.LogInformation("Received PayOS webhook");

                // Verify signature (nếu PayOS cung cấp)
                var signature = Request.Headers["X-PayOS-Signature"].ToString();
                if (!string.IsNullOrEmpty(signature))
                {
                    var isValid = await _paymentService.VerifyPayOSSignatureAsync(webhookData, signature);
                    if (!isValid)
                    {
                        _logger.LogWarning("Invalid PayOS webhook signature");
                        return Unauthorized(new { success = false, message = "Invalid signature" });
                    }
                }

                // Process webhook
                var success = await _paymentService.HandlePayOSWebhookAsync(webhookData);

                if (success)
                {
                    return Ok(new { success = true, message = "Webhook processed" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Webhook processing failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayOS webhook");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy transaction status
        /// </summary>
        [HttpGet("transaction-status/{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetTransactionStatus(string orderId)
        {
            try
            {
                var transaction = await _paymentService.GetTransactionStatusAsync(orderId);

                if (transaction == null)
                {
                    return NotFound(new { success = false, message = "Transaction not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        transactionId = transaction.Id,
                        orderId = transaction.OrderId,
                        totalAmount = transaction.TotalAmount,
                        platformFeeAmount = transaction.PlatformFeeAmount,
                        shopAmount = transaction.ShopAmount,
                        status = transaction.Status.ToString(),
                        paymentMethod = transaction.PaymentMethod,
                        createdAt = transaction.CreatedAt,
                        completedAt = transaction.CompletedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction status for Order {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Cancel payment (nếu PayOS support)
        /// </summary>
        [HttpPost("cancel-payment/{orderId}")]
        [Authorize]
        public async Task<IActionResult> CancelPayment(string orderId)
        {
            try
            {
                var success = await _paymentService.CancelPaymentAsync(orderId);

                if (success)
                {
                    return Ok(new { success = true, message = "Payment cancelled successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Cannot cancel payment" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payment for Order {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }

    /// <summary>
    /// Request DTO cho create payment link
    /// </summary>
    public class CreatePaymentLinkRequest
    {
        public string OrderId { get; set; } = null!;
    }
}