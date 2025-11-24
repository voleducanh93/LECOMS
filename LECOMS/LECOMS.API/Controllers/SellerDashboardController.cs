using LECOMS.Common.Helper;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/seller/dashboard")]
    [Authorize(Roles = "Seller")]
    public class SellerDashboardController : ControllerBase
    {
        private readonly ISellerDashboardService _dashboardService;
        private readonly APIResponse _response;

        public SellerDashboardController(
            ISellerDashboardService dashboardService,
            APIResponse response)
        {
            _dashboardService = dashboardService;
            _response = response;
        }

        private void ResetResponse()
        {
            _response.IsSuccess = true;
            _response.ErrorMessages = new();
            _response.Result = null;
            _response.StatusCode = HttpStatusCode.OK;
        }

        /// <summary>
        /// Seller Dashboard V5
        /// 
        /// Hỗ trợ chọn lịch 1 ngày rồi BE tự tính:
        /// - view = day     -> ngày đó
        /// - view = week    -> tuần chứa ngày đó (Mon-Sun)
        /// - view = month   -> tháng chứa ngày đó
        /// - view = quarter -> quý chứa ngày đó
        /// - view = year    -> năm chứa ngày đó
        /// - view = custom  -> dùng from/to
        ///
        /// Nếu FE chỉ chọn 1 ngày trên DatePicker:
        ///   GET /api/seller/dashboard?view=day&date=2025-11-22
        ///   GET /api/seller/dashboard?view=week&date=2025-11-22
        ///   GET /api/seller/dashboard?view=month&date=2025-11-22
        ///   ...
        ///
        /// Nếu custom range:
        ///   GET /api/seller/dashboard?view=custom&from=2025-11-01&to=2025-11-22
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] string view = "day",
            [FromQuery] DateTime? date = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            ResetResponse();

            try
            {
                var sellerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(sellerUserId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages.Add("Unauthorized");
                    return StatusCode((int)_response.StatusCode, _response);
                }

                // Resolve range từ view + date/from/to
                var (fromDate, toDate, baseDate, normalizedView) =
                    DateRangeHelper.ResolveSellerDashboardRangeV5(view, date, from, to);

                var result = await _dashboardService.GetSellerDashboardAsync(
                    sellerUserId,
                    fromDate,
                    toDate
                );

                // Điền thêm Range info cho FE
                if (result.Range == null)
                    result.Range = new Data.DTOs.Seller.SellerDashboardRangeDTO();

                result.Range.View = normalizedView;
                result.Range.BaseDate = baseDate;
                result.Range.From = fromDate;
                result.Range.To = toDate;

                _response.Result = result;
            }
            catch (ArgumentException ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message);
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
