using LECOMS.Common.Helper;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/admin/notifications")]
    [Authorize(Roles = "Admin")]   // ⭐ chỉ admin mới được broadcast
    public class AdminNotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public AdminNotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public class BroadcastNotificationRequest
        {
            public string Type { get; set; } = "System";   // ví dụ: System, Warning, Event
            public string Title { get; set; } = null!;
            public string? Content { get; set; }
        }

        /// <summary>
        /// Admin gửi thông báo hệ thống cho toàn bộ user
        /// </summary>
        [HttpPost("broadcast")]
        public async Task<IActionResult> Broadcast([FromBody] BroadcastNotificationRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Title))
            {
                return BadRequest(new APIResponse
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    IsSuccess = false,
                    ErrorMessages = new() { "Title is required." }
                });
            }

            var affected = await _notificationService.BroadcastToAllUsersAsync(
                string.IsNullOrWhiteSpace(req.Type) ? "System" : req.Type,
                req.Title,
                req.Content
            );

            return Ok(new APIResponse
            {
                StatusCode = HttpStatusCode.OK,
                IsSuccess = true,
                Result = new
                {
                    SentToUsers = affected
                }
            });
        }
    }
}
