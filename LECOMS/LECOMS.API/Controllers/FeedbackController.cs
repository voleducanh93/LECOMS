using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Feedback;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;
        private readonly APIResponse _response;

        public FeedbackController(IFeedbackService feedbackService, APIResponse response)
        {
            _feedbackService = feedbackService;
            _response = response;
        }

        private void Reset()
        {
            _response.IsSuccess = true;
            _response.ErrorMessages = new();
            _response.Result = null;
            _response.StatusCode = HttpStatusCode.OK;
        }

        // ================================================================
        // CUSTOMER: Create feedback
        // ================================================================
        [HttpPost]
        [Authorize(Roles = "Customer,Seller")]
        public async Task<IActionResult> CreateFeedback(CreateFeedbackRequestDTO dto)
        {
            Reset();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages.Add("Unauthorized");
                    return Unauthorized(_response);
                }

                var result = await _feedbackService.CreateFeedbackAsync(userId, dto);
                _response.Result = result;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)_response.StatusCode, _response);
        }

        // ==========================
        // CUSTOMER/SELLER: Update feedback của mình
        // ==========================
        [HttpPut("{feedbackId}")]
        [Authorize(Roles = "Customer,Seller")]
        public async Task<IActionResult> UpdateFeedback(string feedbackId, [FromBody] UpdateFeedbackRequestDTO dto)
        {
            Reset();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages.Add("Unauthorized");
                    return StatusCode((int)_response.StatusCode, _response);
                }

                var result = await _feedbackService.UpdateFeedbackAsync(userId, feedbackId, dto);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = result;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)_response.StatusCode, _response);
        }

        // ==========================
        // CUSTOMER/SELLER/ADMIN: Delete feedback
        // ==========================
        [HttpDelete("{feedbackId}")]
        [Authorize(Roles = "Customer,Seller,Admin")]
        public async Task<IActionResult> DeleteFeedback(string feedbackId)
        {
            Reset();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages.Add("Unauthorized");
                    return StatusCode((int)_response.StatusCode, _response);
                }

                bool isAdmin = User.IsInRole("Admin");
                bool success;

                if (isAdmin)
                {
                    success = await _feedbackService.DeleteFeedbackByAdminAsync(userId, feedbackId);
                }
                else
                {
                    success = await _feedbackService.DeleteFeedbackByOwnerAsync(userId, feedbackId);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = new { deleted = success };
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)_response.StatusCode, _response);
        }

        // ==========================
        // SELLER: Update reply feedback
        // ==========================
        [HttpPut("{feedbackId}/reply")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateReply(string feedbackId, [FromBody] UpdateReplyFeedbackRequestDTO dto)
        {
            Reset();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages.Add("Unauthorized");
                    return StatusCode((int)_response.StatusCode, _response);
                }

                var result = await _feedbackService.UpdateFeedbackReplyAsync(userId, feedbackId, dto);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = result;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)_response.StatusCode, _response);
        }


        // ==========================
        // SELLER: Delete reply feedback
        // ==========================
        [HttpDelete("{feedbackId}/reply")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> DeleteReply(string feedbackId)
        {
            Reset();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages.Add("Unauthorized");
                    return StatusCode((int)_response.StatusCode, _response);
                }

                var result = await _feedbackService.DeleteFeedbackReplyAsync(userId, feedbackId);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = result;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)_response.StatusCode, _response);
        }


        // ================================================================
        // PUBLIC: Get product feedbacks
        // ================================================================
        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductFeedbacks(string productId, int pageNumber = 1, int pageSize = 10, int? rating = null)
        {
            Reset();

            try
            {
                var list = await _feedbackService.GetProductFeedbacksAsync(productId, rating);

                var totalItems = list.Count();
                var paged = list.Skip((pageNumber - 1) * pageSize).Take(pageSize);

                _response.Result = new
                {
                    items = paged,
                    pagination = new
                    {
                        currentPage = pageNumber,
                        pageSize,
                        totalItems,
                        totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                    }
                };
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)_response.StatusCode, _response);
        }

        // ================================================================
        // SELLER: Get feedback for seller’s shop
        // ================================================================
        [HttpGet("shop/me")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetMyShopFeedbacks(int pageNumber = 1, int pageSize = 10, int? rating = null)
        {
            Reset();

            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var list = await _feedbackService.GetShopFeedbacksForSellerAsync(sellerId!, rating);

                var totalItems = list.Count();
                var paged = list.Skip((pageNumber - 1) * pageSize).Take(pageSize);

                _response.Result = new
                {
                    items = paged,
                    pagination = new
                    {
                        currentPage = pageNumber,
                        pageSize,
                        totalItems,
                        totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                    }
                };
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)_response.StatusCode, _response);
        }

        // ================================================================
        // SELLER: Reply feedback
        // ================================================================
        [HttpPost("{feedbackId}/reply")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> ReplyFeedback(string feedbackId, ReplyFeedbackRequestDTO dto)
        {
            Reset();

            try
            {
                var sellerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var result = await _feedbackService.ReplyFeedbackAsync(sellerUserId!, feedbackId, dto);

                _response.Result = result;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)_response.StatusCode, _response);
        }

        // ================================================================
        // Feedback Summary
        // ================================================================
        [HttpGet("product/{productId}/summary")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductFeedbackSummary(string productId)
        {
            Reset();
            try
            {
                var result = await _feedbackService.GetProductFeedbackSummaryAsync(productId);
                _response.Result = result;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)_response.StatusCode, _response);
        }

        // ================================================================
        // SELLER: feedback report for shop
        // ================================================================
        [HttpGet("shop/me/report")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetShopFeedbackReport()
        {
            Reset();
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var result = await _feedbackService.GetShopFeedbackReportAsync(sellerId);
                _response.Result = result;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)_response.StatusCode, _response);
        }

        [HttpGet("admin/dashboard")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminFeedbackDashboard()
        {
            Reset();

            try
            {
                var result = await _feedbackService.GetAdminFeedbackDashboardAsync();
                _response.Result = result;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)_response.StatusCode, _response);
        }

    }
}
