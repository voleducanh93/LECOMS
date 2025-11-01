using LECOMS.Common.Helper;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/landing-page")]
    public class LandingPageController : ControllerBase
    {
        private readonly ILandingPageService _landingPageService;

        public LandingPageController(ILandingPageService landingPageService)
        {
            _landingPageService = landingPageService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetLandingPageData()
        {
            var response = new APIResponse();
            try
            {
                var data = await _landingPageService.GetLandingPageDataAsync();
                response.StatusCode = HttpStatusCode.OK;
                response.Result = data;
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
