using LECOMS.Common.Helper;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [Route("api/gamification/achievements")]
    [ApiController]
    [Authorize]
    public class AchievementController : ControllerBase
    {
        private readonly IAchievementService _service;
        private readonly UserManager<User> _userManager;

        public AchievementController(IAchievementService service, UserManager<User> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent()
        {
            var userId = _userManager.GetUserId(User);
            var list = await _service.GetRecentBadgesAsync(userId);

            return Ok(new { recentAchievements = list });
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var userId = _userManager.GetUserId(User);
            var list = await _service.GetAllBadgesAsync(userId);

            return Ok(new { achievements = list });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = _userManager.GetUserId(User);
            var list = await _service.GetBadgeHistoryAsync(userId);

            return Ok(new
            {
                history = list,
                total = list.Count()
            });
        }
    }
}
