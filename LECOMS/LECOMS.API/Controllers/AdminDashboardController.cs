using LECOMS.Common.Helper;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace LECOMS.API.Controllers
{
    [Route("api/admin/dashboard")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _dashboardService;
        private readonly APIResponse _response;

        public AdminDashboardController(
            IAdminDashboardService dashboardService,
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
        /// Admin Dashboard API
        /// 
        /// Hỗ trợ chọn lịch tương tự SellerDashboard:
        /// - view = day     -> ngày đó
        /// - view = week    -> tuần chứa ngày đó (Mon-Sun)
        /// - view = month   -> tháng chứa ngày đó
        /// - view = quarter -> quý chứa ngày đó
        /// - view = year    -> năm chứa ngày đó
        /// - view = custom  -> dùng from/to
        ///
        /// Ví dụ:
        ///   GET /api/admin/dashboard?view=day&date=2025-12-05
        ///   GET /api/admin/dashboard?view=week&date=2025-12-05
        ///   GET /api/admin/dashboard?view=month&date=2025-12-05
        ///   GET /api/admin/dashboard?view=custom&from=2025-11-01&to=2025-12-05
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
                // Verify Admin role
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages.Add("Admin access required");
                    return StatusCode((int)_response.StatusCode, _response);
                }

                // Resolve range từ view + date/from/to (sử dụng helper giống SellerDashboard)
                var (fromDate, toDate, baseDate, normalizedView) =
                    DateRangeHelper.ResolveSellerDashboardRangeV5(view, date, from, to);

                var result = await _dashboardService.GetAdminDashboardAsync(
                    fromDate,
                    toDate
                );

                // Điền thêm Range info cho FE
                if (result.Range == null)
                    result.Range = new Data.DTOs.Admin.AdminDashboardRangeDTO();

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
