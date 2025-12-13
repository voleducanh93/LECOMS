using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Shipping;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/shipping")]
    public class ShippingController : ControllerBase
    {
        private const string GHN_BASE_URL =
            "https://online-gateway.ghn.vn/shiip/public-api/";

        private readonly HttpClient _httpClient;
        private readonly ILogger<ShippingController> _logger;

        public ShippingController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ShippingController> logger)
        {
            _logger = logger;

            var ghnToken = configuration["GHN:Token"]
                ?? throw new InvalidOperationException("GHN Token not configured");

            var ghnShopId = configuration["GHN:ShopId"]
                ?? throw new InvalidOperationException("GHN ShopId not configured");

            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(GHN_BASE_URL);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // ✅ BẮT BUỘC CHO GHN
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Token", ghnToken.Trim());
            _httpClient.DefaultRequestHeaders.Add("ShopId", ghnShopId.Trim());
        }

        // ============================================================
        // 1️⃣ GET PROVINCES (PUBLIC)
        // ============================================================
        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            var response = new APIResponse();

            try
            {
                var data = await _httpClient
                    .GetFromJsonAsync<GHNProvinceResponse>("master-data/province");

                if (data?.Code != 200 || data.Data == null)
                    throw new InvalidOperationException(data?.Message);

                response.Result = data.Data;
                response.StatusCode = System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetProvinces failed");
                response.IsSuccess = false;
                response.ErrorMessages.Add("Không thể lấy danh sách tỉnh/thành");
                response.StatusCode = System.Net.HttpStatusCode.ServiceUnavailable;
            }

            return StatusCode((int)response.StatusCode, response);
        }

        // ============================================================
        // 2️⃣ GET DISTRICTS
        // ============================================================
        [HttpGet("districts/{provinceId}")]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            var response = new APIResponse();

            try
            {
                var res = await _httpClient.PostAsJsonAsync(
                    "master-data/district",
                    new { province_id = provinceId });

                var data = await res.Content
                    .ReadFromJsonAsync<GHNDistrictResponse>();

                if (data?.Code != 200 || data.Data == null)
                    throw new InvalidOperationException(data?.Message);

                response.Result = data.Data;
                response.StatusCode = System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDistricts failed");
                response.IsSuccess = false;
                response.ErrorMessages.Add("Không thể lấy danh sách quận/huyện");
                response.StatusCode = System.Net.HttpStatusCode.ServiceUnavailable;
            }

            return StatusCode((int)response.StatusCode, response);
        }

        // ============================================================
        // 3️⃣ GET WARDS
        // ============================================================
        [HttpGet("wards/{districtId}")]
        public async Task<IActionResult> GetWards(int districtId)
        {
            var response = new APIResponse();

            try
            {
                var res = await _httpClient.PostAsJsonAsync(
                    "master-data/ward",
                    new { district_id = districtId });

                var data = await res.Content
                    .ReadFromJsonAsync<GHNWardResponse>();

                if (data?.Code != 200 || data.Data == null)
                    throw new InvalidOperationException(data?.Message);

                response.Result = data.Data;
                response.StatusCode = System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetWards failed");
                response.IsSuccess = false;
                response.ErrorMessages.Add("Không thể lấy danh sách phường/xã");
                response.StatusCode = System.Net.HttpStatusCode.ServiceUnavailable;
            }

            return StatusCode((int)response.StatusCode, response);
        }
    }
}
