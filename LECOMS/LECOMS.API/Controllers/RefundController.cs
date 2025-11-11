using LECOMS.Data.Enum;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LECOMS.API.Controllers
{
    /// <summary>
    /// Controller xử lý hoàn tiền
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RefundController : ControllerBase
    {
        private readonly IRefundService _refundService;
        private readonly ILogger<RefundController> _logger;

        public RefundController(
            IRefundService refundService,
            ILogger<RefundController> logger)
        {
            _refundService = refundService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo refund request
        /// POST: api/refund/create
        /// </summary>
        [HttpPost("create")]
        [Authorize(Roles = "Customer,Seller")]
        public async Task<IActionResult> CreateRefundRequest([FromBody] CreateRefundRequestDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // Set RequestedBy từ current user
                dto.RequestedBy = userId;

                // Validate
                if (string.IsNullOrWhiteSpace(dto.ReasonDescription) || dto.ReasonDescription.Length < 10)
                {
                    return BadRequest(new { success = false, message = "Reason description must be at least 10 characters" });
                }

                var refundRequest = await _refundService.CreateRefundRequestAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "Refund request created successfully. Waiting for admin approval.",
                    data = new
                    {
                        refundId = refundRequest.Id,
                        orderId = refundRequest.OrderId,
                        amount = refundRequest.RefundAmount,
                        recipient = refundRequest.Recipient.ToString(),
                        status = refundRequest.Status.ToString(),
                        requestedAt = refundRequest.RequestedAt
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation creating refund request");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized refund request");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refund request");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy refund requests của user
        /// GET: api/refund/my-requests?page=1&pageSize=20
        /// </summary>
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRefundRequests(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var refundRequests = await _refundService.GetRefundRequestsByUserAsync(userId, page, pageSize);

                return Ok(new
                {
                    success = true,
                    data = refundRequests.Select(r => new
                    {
                        id = r.Id,
                        orderId = r.OrderId,
                        orderCode = r.Order?.OrderCode,
                        amount = r.RefundAmount,
                        type = r.Type.ToString(),
                        recipient = r.Recipient.ToString(),
                        reasonType = r.ReasonType.ToString(),
                        reasonDescription = r.ReasonDescription,
                        status = r.Status.ToString(),
                        requestedAt = r.RequestedAt,
                        processedAt = r.ProcessedAt,
                        processNote = r.ProcessNote
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user refund requests");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy refund requests theo order
        /// GET: api/refund/by-order/{orderId}
        /// </summary>
        [HttpGet("by-order/{orderId}")]
        public async Task<IActionResult> GetRefundRequestsByOrder(string orderId)
        {
            try
            {
                var refundRequests = await _refundService.GetRefundRequestsByOrderAsync(orderId);

                return Ok(new
                {
                    success = true,
                    data = refundRequests.Select(r => new
                    {
                        id = r.Id,
                        amount = r.RefundAmount,
                        type = r.Type.ToString(),
                        recipient = r.Recipient.ToString(),
                        reasonType = r.ReasonType.ToString(),
                        reasonDescription = r.ReasonDescription,
                        status = r.Status.ToString(),
                        requestedBy = r.RequestedByUser?.UserName,
                        requestedAt = r.RequestedAt,
                        processedBy = r.ProcessedByUser?.UserName,
                        processedAt = r.ProcessedAt,
                        processNote = r.ProcessNote
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refund requests by order");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy refund request detail
        /// GET: api/refund/{refundId}
        /// </summary>
        [HttpGet("{refundId}")]
        public async Task<IActionResult> GetRefundRequest(string refundId)
        {
            try
            {
                var refund = await _refundService.GetRefundRequestAsync(refundId);

                if (refund == null)
                {
                    return NotFound(new { success = false, message = "Refund request not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = refund.Id,
                        orderId = refund.OrderId,
                        orderCode = refund.Order?.OrderCode,
                        amount = refund.RefundAmount,
                        type = refund.Type.ToString(),
                        recipient = refund.Recipient.ToString(),
                        reasonType = refund.ReasonType.ToString(),
                        reasonDescription = refund.ReasonDescription,
                        attachmentUrls = refund.AttachmentUrls,
                        status = refund.Status.ToString(),
                        requestedBy = new
                        {
                            id = refund.RequestedBy,
                            name = refund.RequestedByUser?.UserName
                        },
                        requestedAt = refund.RequestedAt,
                        processedBy = refund.ProcessedBy != null ? new
                        {
                            id = refund.ProcessedBy,
                            name = refund.ProcessedByUser?.UserName
                        } : null,
                        processedAt = refund.ProcessedAt,
                        processNote = refund.ProcessNote
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refund request {RefundId}", refundId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // ==================== ADMIN ENDPOINTS ====================

        /// <summary>
        /// Admin: Lấy pending refund requests
        /// GET: api/refund/admin/pending
        /// </summary>
        [HttpGet("admin/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingRefundRequests()
        {
            try
            {
                var refundRequests = await _refundService.GetPendingRefundRequestsAsync();

                return Ok(new
                {
                    success = true,
                    data = refundRequests.Select(r => new
                    {
                        id = r.Id,
                        orderId = r.OrderId,
                        orderCode = r.Order?.OrderCode,
                        shopName = r.Order?.Shop?.Name,
                        customerName = r.Order?.User?.UserName,
                        amount = r.RefundAmount,
                        type = r.Type.ToString(),
                        recipient = r.Recipient.ToString(),
                        reasonType = r.ReasonType.ToString(),
                        reasonDescription = r.ReasonDescription,
                        attachmentUrls = r.AttachmentUrls,
                        requestedBy = r.RequestedByUser?.UserName,
                        requestedAt = r.RequestedAt
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending refund requests");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Approve refund request
        /// POST: api/refund/admin/approve/{refundId}
        /// </summary>
        [HttpPost("admin/approve/{refundId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveRefund(
            string refundId,
            [FromBody] AdminRefundActionRequest request)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized();

                var refund = await _refundService.ApproveRefundAsync(refundId, adminId, request.Note);

                return Ok(new
                {
                    success = true,
                    message = "Refund approved and processed successfully",
                    data = new
                    {
                        refundId = refund.Id,
                        status = refund.Status.ToString(),
                        processedAt = refund.ProcessedAt
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation approving refund");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving refund {RefundId}", refundId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Reject refund request
        /// POST: api/refund/admin/reject/{refundId}
        /// </summary>
        [HttpPost("admin/reject/{refundId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectRefund(
            string refundId,
            [FromBody] AdminRefundActionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new { success = false, message = "Rejection reason is required" });
                }

                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized();

                var refund = await _refundService.RejectRefundAsync(refundId, adminId, request.Reason);

                return Ok(new
                {
                    success = true,
                    message = "Refund rejected successfully",
                    data = new
                    {
                        refundId = refund.Id,
                        status = refund.Status.ToString(),
                        processedAt = refund.ProcessedAt
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation rejecting refund");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting refund {RefundId}", refundId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }

    /// <summary>
    /// Request DTO cho admin approve/reject
    /// </summary>
    public class AdminRefundActionRequest
    {
        public string? Note { get; set; }
        public string? Reason { get; set; }
    }
}