using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Shop;
using LECOMS.Service.Services;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/shop/address")]
    [Authorize]
    public class ShopAddressController : ControllerBase
    {
        private readonly IShopAddressService _service;
        private readonly IShopService _shopService;

        public ShopAddressController(IShopAddressService service, IShopService shopService)
        {
            _service = service;
            _shopService = shopService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyShopAddress()
        {
            var response = new APIResponse();
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                response.Result = await _service.GetMyShopAddressAsync(sellerId);
                response.StatusCode = System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
            }

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPost("me")]
        public async Task<IActionResult> UpsertMyShopAddress(
            [FromBody] UpsertShopAddressDTO dto)
        {
            var response = new APIResponse();
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                response.Result = await _service.UpsertMyShopAddressAsync(sellerId, dto);
                response.StatusCode = System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
            }

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPut("me/{addressId}")]
        public async Task<IActionResult> UpdateMyShopAddress( int addressId, [FromBody] UpsertShopAddressDTO dto)
        {
            var response = new APIResponse();
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                response.Result = await _service.UpdateMyShopAddressAsync(
                    sellerId,
                    addressId,
                    dto);

                response.StatusCode = System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
            }

            return StatusCode((int)response.StatusCode, response);
        }

        [Authorize]
        [HttpPost("me/ghn/connect")]
        public async Task<IActionResult> ConnectGHN([FromBody] ConnectGHNRequestDTO dto)
        {
            var response = new APIResponse();
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                await _shopService.ConnectGHNAsync(sellerId, dto);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = "Kết nối GHN thành công";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
                response.StatusCode = HttpStatusCode.BadRequest;
            }

            return StatusCode((int)response.StatusCode, response);
        }

    }
}
