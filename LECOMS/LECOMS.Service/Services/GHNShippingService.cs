using LECOMS.Data.DTOs.Shipping;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class GHNShippingService : IShippingService
    {
        private const string GHN_BASE_URL =
            "https://online-gateway.ghn.vn/shiip/public-api/";

        private readonly HttpClient _httpClient;
        private readonly ILogger<GHNShippingService> _logger;

        public GHNShippingService(HttpClient httpClient, ILogger<GHNShippingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(GHN_BASE_URL);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }


        // =====================================================
        // SIMPLE FEE (wrapper)
        // =====================================================
        public async Task<decimal> CalculateShippingFeeAsync(
    string ghnToken,
    string ghnShopId,
    int fromDistrictId,
    string fromWardCode,
    int toDistrictId,
    string toWardCode,
    int weight,
    decimal orderValue,
    int serviceTypeId = 2)
        {
            var result = await GetShippingDetailsAsync(
                ghnToken,
                ghnShopId,
                fromDistrictId,
                fromWardCode,
                toDistrictId,
                toWardCode,
                weight,
                orderValue,
                serviceTypeId);

            if (result == null)
                throw new InvalidOperationException(
                    "Không thể tính phí vận chuyển.");

            return result.ShippingFee;
        }

        // =====================================================
        // FULL GHN FEE
        // =====================================================
        public async Task<ShippingFeeCalculationDTO?> GetShippingDetailsAsync(
    string ghnToken,
    string ghnShopId,

    int fromDistrictId,
    string fromWardCode,
    int toDistrictId,
    string toWardCode,
    int weight,
    decimal orderValue,
    int serviceTypeId = 2,
    int? length = null,
    int? width = null,
    int? height = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ghnToken) ||
                    string.IsNullOrWhiteSpace(ghnShopId))
                {
                    throw new InvalidOperationException(
                        "Shop chưa cấu hình GHN Token / ShopId.");
                }

                var request = new GHNCalculateFeeRequest
                {
                    FromDistrictId = fromDistrictId,
                    FromWardCode = fromWardCode,
                    ToDistrictId = toDistrictId,
                    ToWardCode = toWardCode,
                    Weight = weight > 0 ? weight : 500,
                    Length = length ?? 20,
                    Width = width ?? 15,
                    Height = height ?? 10,
                    InsuranceValue = (int)Math.Min(orderValue, 5_000_000),
                    ServiceTypeId = serviceTypeId
                };

                _logger.LogInformation(
                    "🚚 GHN Fee Request | ShopId={ShopId} | {FromDistrict}/{FromWard} → {ToDistrict}/{ToWard}",
                    ghnShopId, fromDistrictId, fromWardCode, toDistrictId, toWardCode);

                var httpRequest = new HttpRequestMessage( HttpMethod.Post, "v2/shipping-order/fee")
                {
                    Content = JsonContent.Create(request)
                };

                httpRequest.Headers.Add("Token", ghnToken.Trim());
                httpRequest.Headers.Add("ShopId", ghnShopId.Trim());

                var response = await _httpClient.SendAsync(httpRequest);

                var raw = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "❌ GHN Error | ShopId={ShopId} | {Raw}",
                        ghnShopId, raw);

                    throw new InvalidOperationException(
                        "GHN không thể tính phí vận chuyển cho địa chỉ này.");
                }

                var ghn = await response.Content
                    .ReadFromJsonAsync<GHNCalculateFeeResponse>();

                if (ghn?.Code != 200 || ghn.Data == null)
                {
                    throw new InvalidOperationException(
                        ghn?.Message ?? "GHN API lỗi.");
                }

                return new ShippingFeeCalculationDTO
                {
                    ShippingFee = ghn.Data.Total,
                    ExpectedDeliveryTime =
                        ghn.Data.ExpectedDeliveryTime ?? "2-3 ngày",
                    Provider = "GHN",
                    ServiceTypeId = serviceTypeId,
                    ServiceTypeName =
                        serviceTypeId == 2 ? "Express" : "Standard"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ GHN fee calculation failed");
                throw;
            }
        }

    }
}
