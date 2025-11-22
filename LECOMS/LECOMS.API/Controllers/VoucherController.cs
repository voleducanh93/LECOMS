using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Voucher;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/vouchers")]
    [Authorize] // tất cả endpoint đều yêu cầu login
    public class VoucherController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VoucherController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        private string? GetUserId()
        {
            // Tùy bạn map claim: thường là NameIdentifier
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("uid");
        }

        // =========================================================
        // 1. Lấy danh sách voucher của user (Voucher wallet)
        // GET /api/vouchers/my
        // =========================================================
        [HttpGet("my")]
        public async Task<IActionResult> GetMyVouchers()
        {
            var response = new APIResponse();

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized.");
                    return StatusCode((int)response.StatusCode, response);
                }

                var vouchers = await _voucherService.GetMyVouchersAsync(userId);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = vouchers;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        // =========================================================
        // 2. Preview voucher trước khi checkout
        // POST /api/vouchers/preview
        //
        // Body:
        // {
        //   "voucherCode": "LECOM10",
        //   "orders": [
        //     { "orderId": "shop1", "subtotal": 150000, "shippingFee": 30000 },
        //     { "orderId": "shop2", "subtotal": 200000, "shippingFee": 30000 }
        //   ]
        // }
        //
        // → Trả về VoucherApplyResultDTO (TotalDiscount + discount per order)
        // =========================================================
        [HttpPost("preview")]
        public async Task<IActionResult> PreviewVoucher([FromBody] VoucherPreviewRequestDTO dto)
        {
            var response = new APIResponse();

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized.");
                    return StatusCode((int)response.StatusCode, response);
                }

                if (dto == null ||
                    string.IsNullOrWhiteSpace(dto.VoucherCode) ||
                    dto.Orders == null ||
                    !dto.Orders.Any())
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.ErrorMessages.Add("VoucherCode và Orders là bắt buộc.");
                    return StatusCode((int)response.StatusCode, response);
                }

                // Map DTO → dummy Order entities cho VoucherService xử lý
                var orders = dto.Orders.Select(o => new Order
                {
                    Id = string.IsNullOrWhiteSpace(o.OrderId)
                        ? Guid.NewGuid().ToString()
                        : o.OrderId,
                    Subtotal = o.Subtotal,
                    ShippingFee = o.ShippingFee,
                    Discount = 0,
                    Total = o.Subtotal + o.ShippingFee
                }).ToList();

                var result = await _voucherService.ValidateAndPreviewAsync(
                    userId,
                    dto.VoucherCode,
                    orders
                );

                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }
    }
}
