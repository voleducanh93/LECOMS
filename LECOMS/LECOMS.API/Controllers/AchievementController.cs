using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Gamification;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LECOMS.API.Controllers.Gamification
{
    [Route("api/gamification/achievements")]
    [ApiController]
    [Authorize]
    public class AchievementController : ControllerBase
    {
        private readonly IAchievementService _service;
        private readonly UserManager<User> _userManager;

        public AchievementController(
            IAchievementService service,
            UserManager<User> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        // =============================================================
        // 1) GET ALL ACHIEVEMENTS
        // =============================================================
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var userId = _userManager.GetUserId(User);
            var list = await _service.GetAllAsync(userId);

            return Ok(new { achievements = list });
        }

        // =============================================================
        // 2) GET RECENT
        // =============================================================
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent()
        {
            var userId = _userManager.GetUserId(User);
            var list = await _service.GetRecentAsync(userId);

            return Ok(new { recentAchievements = list });
        }

        // =============================================================
        // 3) GET HISTORY
        // =============================================================
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = _userManager.GetUserId(User);
            var history = await _service.GetHistoryAsync(userId);

            return Ok(new
            {
                total = history.Count(),
                history
            });
        }

        // =============================================================
        // 4) CLAIM REWARD
        // =============================================================
        [HttpPost("{id}/claim")]
        public async Task<IActionResult> Claim(int id)
        {
            var userId = _userManager.GetUserId(User);

            var success = await _service.ClaimRewardAsync(userId, id);

            if (!success)
                return BadRequest(new
                {
                    message = "Cannot claim reward. Achievement not completed or already claimed."
                });

            return Ok(new { message = "Reward claimed successfully!" });
        }
    }
}
