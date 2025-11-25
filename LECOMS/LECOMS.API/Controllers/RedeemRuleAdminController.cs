using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Gamification.LECOMS.Data.DTOs.Gamification;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/admin/redeem-rules")]
    [Authorize(Roles = "Admin")]
    public class RedeemRuleAdminController : ControllerBase
    {
        private readonly IGamificationAdminService _service;

        public RedeemRuleAdminController(IGamificationAdminService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = new APIResponse();
            try
            {
                response.Result = await _service.GetRedeemRulesAsync();
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var response = new APIResponse();
            try
            {
                var result = await _service.GetRedeemRuleByIdAsync(id);

                if (result == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Redeem rule không tìm thấy.");
                    return StatusCode((int)response.StatusCode, response);
                }

                response.Result = result;
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RedeemRuleCreateDTO dto)
        {
            var response = new APIResponse();
            try
            {
                response.Result = await _service.CreateRedeemRuleAsync(dto);
                response.StatusCode = HttpStatusCode.Created;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] RedeemRuleUpdateDTO dto)
        {
            var response = new APIResponse();
            try
            {
                response.Result = await _service.UpdateRedeemRuleAsync(id, dto);
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var response = new APIResponse();
            try
            {
                var ok = await _service.DeleteRedeemRuleAsync(id);
                response.Result = ok;
                response.StatusCode = ok ? HttpStatusCode.OK : HttpStatusCode.NotFound;
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
