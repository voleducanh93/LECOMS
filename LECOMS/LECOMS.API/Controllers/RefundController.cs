using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Refund;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LECOMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefundController : ControllerBase
    {
        private readonly IRefundService _refundService;
        private readonly APIResponse _response;

        public RefundController(IRefundService refundService, APIResponse response)
        {
            _refundService = refundService;
            _response = response;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("UserId not found in token.");
        }

        // ===========================
        // Request DTOs (local)
        // ===========================

        public class SellerDecisionDTO
        {
            public bool Approve { get; set; }
            public string? RejectReason { get; set; }
        }

        public class AdminDecisionDTO
        {
            public bool Approve { get; set; }
            public string? RejectReason { get; set; }
        }

        public class UploadEvidenceDTO
        {
            /// <summary>
            /// Danh sách URL ảnh/video (Cloudinary, v.v.) mà FE đã upload sẵn
            /// </summary>
            public string[] Urls { get; set; } = Array.Empty<string>();
        }

        // ===========================
        // CUSTOMER
        // ===========================

        /// <summary>
        /// Customer tạo yêu cầu refund
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Customer, Seller", AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> CreateRefund([FromBody] CreateRefundRequestDTO dto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _refundService.CreateAsync(userId, dto);

                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = result;
                return StatusCode((int)_response.StatusCode, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }

        /// <summary>
        /// Customer xem danh sách refund của mình
        /// </summary>
        [HttpGet("my")]
        [Authorize(Roles = "Customer, Seller", AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetMyRefunds()
        {
            try
            {
                var userId = GetUserId();
                var result = await _refundService.GetMyAsync(userId);

                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = result;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }

        /// <summary>
        /// Customer upload / cập nhật evidence (ảnh/video) cho refund
        /// FE phải upload file lên Cloudinary trước, rồi gửi URL vào đây.
        /// </summary>
        [HttpPost("{refundId}/evidence")]
        [Authorize(Roles = "Customer, Seller", AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> UploadEvidence(string refundId, [FromBody] UploadEvidenceDTO dto)
        {
            try
            {
                var customerId = GetUserId();
                var result = await _refundService.AddEvidenceAsync(refundId, customerId, dto.Urls);

                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = result;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }

        // ===========================
        // SELLER
        // ===========================

        /// <summary>
        /// Seller xem các refund liên quan đến shop của mình
        /// </summary>
        [HttpGet("seller")]
        [Authorize(Roles = "Seller", AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetShopRefunds()
        {
            try
            {
                var sellerId = GetUserId();
                var result = await _refundService.GetForShopAsync(sellerId);

                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = result;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }

        /// <summary>
        /// Seller duyệt / từ chối refund
        /// </summary>
        [HttpPost("seller/{refundId}/decision")]
        [Authorize(Roles = "Seller", AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> SellerDecision(string refundId, [FromBody] SellerDecisionDTO dto)
        {
            try
            {
                var sellerId = GetUserId();
                var result = await _refundService.SellerDecisionAsync(
                    refundId,
                    sellerId,
                    dto.Approve,
                    dto.RejectReason);

                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = result;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }

        // ===========================
        // ADMIN
        // ===========================

        /// <summary>
        /// Admin xem danh sách refund chờ duyệt
        /// </summary>
        [HttpGet("admin/pending")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetPendingAdmin()
        {
            try
            {
                var result = await _refundService.GetPendingAdminAsync();

                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = result;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }

        /// <summary>
        /// Admin approve / reject refund (refund sẽ trả tiền vào Customer Wallet)
        /// </summary>
        [HttpPost("admin/{refundId}/decision")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AdminDecision(string refundId, [FromBody] AdminDecisionDTO dto)
        {
            try
            {
                var adminId = GetUserId();
                var result = await _refundService.AdminDecisionAsync(
                    refundId,
                    adminId,
                    dto.Approve,
                    dto.RejectReason);

                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = result;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }

        /// <summary>
        /// Admin dashboard thống kê refund (tổng số, theo trạng thái, tổng tiền, 30 ngày gần nhất)
        /// </summary>
        [HttpGet("admin/dashboard")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            try
            {
                var result = await _refundService.GetAdminDashboardAsync();

                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = result;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }
    }
    public class UploadEvidenceDTO
    {
        /// <summary>
        /// Danh sách URL ảnh/video (Cloudinary, v.v.) mà FE đã upload sẵn
        /// </summary>
        public string[] Urls { get; set; } = Array.Empty<string>();
    }

}
