using Azure;
using LECOMS.Common.Helper;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
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
        /// <param name="request">Request chứa OrderId</param>
        /// <returns>Payment URL để redirect customer</returns>
        [HttpPost("create-payment-link")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentLinkRequest request)
        {
            var response = new APIResponse();  // 🔥 thêm response

            try
            {
                // Lấy full result (orders + total + shipping + discount + paymentUrl)
                var result = await _paymentService.CreatePaymentResultForExistingOrdersAsync(request.OrderId);

                response.StatusCode = HttpStatusCode.Created;
                response.IsSuccess = true;
                response.Result = result;

                return StatusCode((int)response.StatusCode, response);
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("Internal server error");
                response.ErrorMessages.Add(ex.Message);

                return StatusCode(500, response);
            }
        }


        /// <summary>
        /// Webhook callback từ PayOS
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous] // PayOS call không cần auth
        public async Task<IActionResult> PayOSWebhook()
        {
            try
            {
                _logger.LogInformation("=== RECEIVED PAYOS WEBHOOK ===");
                _logger.LogInformation("Timestamp: {Timestamp} UTC", DateTime.UtcNow);
                _logger.LogInformation("User: {User}", User?.Identity?.Name ?? "Anonymous");

                // Read raw body
                using var reader = new System.IO.StreamReader(Request.Body);
                var webhookData = await reader.ReadToEndAsync();

                _logger.LogInformation("Webhook Raw Data: {Data}", webhookData);

                // ✅ HANDLE EMPTY BODY (PayOS test request)
                if (string.IsNullOrWhiteSpace(webhookData) || webhookData == "{}" || webhookData == "")
                {
                    _logger.LogInformation("Empty webhook body - PayOS test request");
                    return Ok(new
                    {
                        success = true,
                        message = "Webhook endpoint is healthy"
                    });
                }

                // Verify signature (nếu PayOS cung cấp)
                var signature = Request.Headers["X-PayOS-Signature"].ToString();
                if (!string.IsNullOrEmpty(signature))
                {
                    _logger.LogInformation("Verifying signature: {Signature}", signature);
                    var isValid = await _paymentService.VerifyPayOSSignatureAsync(webhookData, signature);
                    if (!isValid)
                    {
                        _logger.LogWarning("Invalid PayOS webhook signature");
                        // ✅ Still return 200 OK (but log warning)
                        return Ok(new
                        {
                            success = false,
                            message = "Invalid signature - but acknowledged"
                        });
                    }
                }

                // Process webhook
                var success = await _paymentService.HandlePayOSWebhookAsync(webhookData);

                // ✅ ALWAYS RETURN 200 OK
                if (success)
                {
                    _logger.LogInformation("=== ✅ WEBHOOK PROCESSED SUCCESSFULLY ===");
                    return Ok(new { success = true, message = "Webhook processed" });
                }
                else
                {
                    _logger.LogWarning("Webhook processing returned false");
                    return Ok(new
                    {
                        success = false,
                        message = "Webhook processing failed - but acknowledged"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing PayOS webhook");
                // ✅ RETURN 200 OK EVEN ON ERROR
                return Ok(new
                {
                    success = false,
                    message = "Internal server error - but acknowledged",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Health check for webhook endpoint
        /// Test URL: https://lecom.click/api/Payment/webhook/health
        /// </summary>
        [HttpGet("webhook/health")]
        [AllowAnonymous]
        public IActionResult WebhookHealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                endpoint = "/api/Payment/webhook",
                timestamp = DateTime.UtcNow,
                message = "Webhook endpoint is operational",
                server = Environment.MachineName,
                user = "haupdse170479"
            });
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