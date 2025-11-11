using LECOMS.Data.DTOs.Refund;
using LECOMS.RepositoryContract.Interfaces;
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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RefundController : ControllerBase
    {
        private readonly IRefundService _refundService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RefundController> _logger;

        public RefundController(
            IRefundService refundService,
            IUnitOfWork unitOfWork,
            ILogger<RefundController> logger)
        {
            _refundService = refundService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // ==================== CUSTOMER ENDPOINTS ====================

        /// <summary>
        /// Customer tạo refund request
        /// </summary>
        [HttpPost("create")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateRefundRequest([FromBody] CreateRefundRequestDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                dto.RequestedBy = userId;

                if (string.IsNullOrWhiteSpace(dto.ReasonDescription) || dto.ReasonDescription.Length < 10)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Reason description must be at least 10 characters"
                    });
                }

                var refundRequest = await _refundService.CreateRefundRequestAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "Refund request created. Shop will review and respond.",
                    data = new
                    {
                        refundId = refundRequest.Id,
                        orderId = refundRequest.OrderId,
                        amount = refundRequest.RefundAmount,
                        status = refundRequest.Status.ToString(),
                        requestedAt = refundRequest.RequestedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refund request");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Customer xem refund requests của mình
        /// </summary>
        [HttpGet("my-requests")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyRefundRequests(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var refunds = await _refundService.GetRefundRequestsByUserAsync(userId, page, pageSize);

                return Ok(new
                {
                    success = true,
                    data = refunds.Select(r => new
                    {
                        id = r.Id,
                        orderId = r.OrderId,
                        orderCode = r.Order?.OrderCode,
                        amount = r.RefundAmount,
                        type = r.Type.ToString(),
                        reasonType = r.ReasonType.ToString(),
                        reasonDescription = r.ReasonDescription,
                        status = r.Status.ToString(),
                        requestedAt = r.RequestedAt,
                        shopRespondedAt = r.ShopRespondedAt,
                        shopRejectReason = r.ShopRejectReason,
                        processedAt = r.ProcessedAt
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refund requests");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Xem chi tiết refund request
        /// </summary>
        [HttpGet("{refundId}")]
        public async Task<IActionResult> GetRefundRequest(string refundId)
        {
            try
            {
                var refund = await _refundService.GetRefundRequestAsync(refundId);

                if (refund == null)
                    return NotFound(new { success = false, message = "Refund request not found" });

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
                        shopRespondedBy = refund.ShopResponseByUser?.UserName,
                        shopRespondedAt = refund.ShopRespondedAt,
                        shopRejectReason = refund.ShopRejectReason,
                        processedAt = refund.ProcessedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refund request");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Customer hủy refund request
        /// </summary>
        [HttpPost("{refundId}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CancelRefundRequest(string refundId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var refund = await _refundService.CancelRefundRequestAsync(refundId, userId);

                return Ok(new
                {
                    success = true,
                    message = "Refund request cancelled",
                    data = new
                    {
                        refundId = refund.Id,
                        status = refund.Status.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling refund");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ==================== SHOP ENDPOINTS ====================

        /// <summary>
        /// Shop xem pending refund requests
        /// </summary>
        [HttpGet("shop/pending")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetShopPendingRefunds()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == userId);
                if (shop == null)
                    return NotFound(new { success = false, message = "Shop not found" });

                var refunds = await _refundService.GetShopPendingRefundsAsync(shop.Id);

                return Ok(new
                {
                    success = true,
                    data = refunds.Select(r => new
                    {
                        id = r.Id,
                        orderId = r.OrderId,
                        orderCode = r.Order?.OrderCode,
                        customerName = r.RequestedByUser?.UserName,
                        amount = r.RefundAmount,
                        reasonType = r.ReasonType.ToString(),
                        reasonDescription = r.ReasonDescription,
                        attachmentUrls = r.AttachmentUrls,
                        requestedAt = r.RequestedAt
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shop pending refunds");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Shop approve refund
        /// </summary>
        [HttpPost("shop/approve/{refundId}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> ShopApproveRefund(string refundId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var refund = await _refundService.ShopApproveRefundAsync(refundId, userId);

                return Ok(new
                {
                    success = true,
                    message = "Refund approved. Customer has been refunded.",
                    data = new
                    {
                        refundId = refund.Id,
                        status = refund.Status.ToString(),
                        respondedAt = refund.ShopRespondedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving refund");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Shop reject refund
        /// </summary>
        [HttpPost("shop/reject/{refundId}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> ShopRejectRefund(
            string refundId,
            [FromBody] ShopRejectRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 20)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Reject reason must be at least 20 characters"
                    });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var refund = await _refundService.ShopRejectRefundAsync(refundId, userId, request.Reason);

                return Ok(new
                {
                    success = true,
                    message = "Refund rejected. Customer can review your shop.",
                    data = new
                    {
                        refundId = refund.Id,
                        status = refund.Status.ToString(),
                        respondedAt = refund.ShopRespondedAt,
                        rejectReason = refund.ShopRejectReason
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting refund");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Shop xem lịch sử refund
        /// </summary>
        [HttpGet("shop/history")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetShopRefundHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == userId);
                if (shop == null)
                    return NotFound(new { success = false, message = "Shop not found" });

                var refunds = await _refundService.GetShopRefundHistoryAsync(shop.Id, page, pageSize);

                return Ok(new
                {
                    success = true,
                    data = refunds.Select(r => new
                    {
                        id = r.Id,
                        orderId = r.OrderId,
                        orderCode = r.Order?.OrderCode,
                        customerName = r.RequestedByUser?.UserName,
                        amount = r.RefundAmount,
                        reasonType = r.ReasonType.ToString(),
                        status = r.Status.ToString(),
                        requestedAt = r.RequestedAt,
                        respondedAt = r.ShopRespondedAt
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shop refund history");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Shop xem thống kê refund
        /// </summary>
        [HttpGet("shop/statistics")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetShopRefundStatistics()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == userId);
                if (shop == null)
                    return NotFound(new { success = false, message = "Shop not found" });

                var stats = await _refundService.GetShopRefundStatisticsAsync(shop.Id);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalRequests = stats.TotalRequests,
                        approvedCount = stats.ApprovedCount,
                        rejectedCount = stats.RejectedCount,
                        approvalRate = $"{stats.ApprovalRate:P0}",
                        responseRate = $"{stats.ResponseRate:P0}",
                        avgResponseTimeHours = Math.Round(stats.AverageResponseTimeHours, 1)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shop refund statistics");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }

    public class ShopRejectRequest
    {
        public string Reason { get; set; } = null!;
    }
}