using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Gamification;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Security.Claims;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/gamification")]
    [Authorize] // chỉ user đã login
    public class GamificationController : ControllerBase
    {
        private readonly IGamificationService _service;

        public GamificationController(IGamificationService service)
        {
            _service = service;
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User không tìm thấy");

        /// <summary>Dashboard gamification: level, coins, quests</summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var response = new APIResponse();
            try
            {
                var userId = GetUserId();
                var result = await _service.GetProfileAsync(userId);
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

        /// <summary>User claim phần thưởng của quest</summary>
        [HttpPost("quests/{userQuestId}/claim")]
        public async Task<IActionResult> ClaimQuest(string userQuestId)
        {
            var response = new APIResponse();
            try
            {
                var userId = GetUserId();
                var ok = await _service.ClaimQuestAsync(userId, userQuestId);
                response.StatusCode = ok ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                response.Result = ok;
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>Rewards Store: boosters + vouchers</summary>
        [HttpGet("rewards")]
        public async Task<IActionResult> GetRewardsStore()
        {
            var response = new APIResponse();
            try
            {
                var userId = GetUserId();
                var result = await _service.GetRewardsStoreAsync(userId);
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
        ///// <summary>Gửi event gamification (complete lesson, purchase,...)</summary>
        //[HttpPost("events")]
        //public async Task<IActionResult> HandleEvent([FromBody] GamificationEventDTO dto)
        //{
        //    var response = new APIResponse();
        //    try
        //    {
        //        var userId = GetUserId();
        //        await _service.HandleEventAsync(userId, dto);
        //        response.StatusCode = HttpStatusCode.OK;
        //        response.Result = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.IsSuccess = false;
        //        response.StatusCode = HttpStatusCode.InternalServerError;
        //        response.ErrorMessages.Add(ex.Message);
        //    }
        //    return StatusCode((int)response.StatusCode, response);
        //}

        /// <summary>Leaderboard Weekly/Monthly/All</summary>
        [HttpGet("leaderboard")]
        [AllowAnonymous] // nếu bạn muốn public
        public async Task<IActionResult> GetLeaderboard([FromQuery] string period = "weekly")
        {
            var response = new APIResponse();
            try
            {
                var userId = User.Identity?.IsAuthenticated == true ? GetUserId() : "";
                var result = await _service.GetLeaderboardAsync(userId, period);
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

        /// <summary>Redeem booster / voucher từ coins</summary>
        [HttpPost("redeem")]
        public async Task<IActionResult> Redeem([FromBody] RedeemRequestDTO dto)
        {
            var response = new APIResponse();
            try
            {
                var userId = GetUserId();
                var result = await _service.RedeemAsync(userId, dto);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
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
