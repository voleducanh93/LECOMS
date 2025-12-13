using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Shipping
{
    // =========================================================
    // REQUEST TÍNH PHÍ SHIP
    // =========================================================
    public class GHNCalculateFeeRequest
    {
        [JsonPropertyName("from_district_id")]
        public int FromDistrictId { get; set; }

        [JsonPropertyName("from_ward_code")]
        public string FromWardCode { get; set; } = null!;

        [JsonPropertyName("to_district_id")]
        public int ToDistrictId { get; set; }

        [JsonPropertyName("to_ward_code")]
        public string ToWardCode { get; set; } = null!;

        [JsonPropertyName("weight")]
        public int Weight { get; set; }

        [JsonPropertyName("length")]
        public int? Length { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("insurance_value")]
        public int InsuranceValue { get; set; }

        [JsonPropertyName("service_type_id")]
        public int ServiceTypeId { get; set; } = 2;

        [JsonPropertyName("coupon")]
        public string? Coupon { get; set; }
    }

    // =========================================================
    // RESPONSE TỪ GHN
    // =========================================================
    public class GHNCalculateFeeResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public GHNFeeData? Data { get; set; }
    }

    public class GHNFeeData
    {
        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("service_fee")]
        public decimal ServiceFee { get; set; }

        [JsonPropertyName("insurance_fee")]
        public decimal InsuranceFee { get; set; }

        [JsonPropertyName("pick_station_fee")]
        public decimal PickStationFee { get; set; }

        [JsonPropertyName("coupon_value")]
        public decimal CouponValue { get; set; }

        [JsonPropertyName("r2s_fee")]
        public decimal R2SFee { get; set; }

        [JsonPropertyName("expected_delivery_time")]
        public string? ExpectedDeliveryTime { get; set; }
    }

    // =========================================================
    // PROVINCE
    // =========================================================
    public class GHNProvinceResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public List<GHNProvince>? Data { get; set; }
    }

    public class GHNProvince
    {
        [JsonPropertyName("ProvinceID")]
        public int ProvinceID { get; set; }

        [JsonPropertyName("ProvinceName")]
        public string ProvinceName { get; set; } = null!;

        [JsonPropertyName("Code")]
        public string? Code { get; set; }
    }

    // =========================================================
    // DISTRICT
    // =========================================================
    public class GHNDistrictResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public List<GHNDistrict>? Data { get; set; }
    }

    public class GHNDistrict
    {
        [JsonPropertyName("DistrictID")]
        public int DistrictID { get; set; }

        [JsonPropertyName("ProvinceID")]
        public int ProvinceID { get; set; }

        [JsonPropertyName("DistrictName")]
        public string DistrictName { get; set; } = null!;

        [JsonPropertyName("Code")]
        public string? Code { get; set; }
    }

    // =========================================================
    // WARD
    // =========================================================
    public class GHNWardResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public List<GHNWard>? Data { get; set; }
    }

    public class GHNWard
    {
        [JsonPropertyName("WardCode")]
        public string WardCode { get; set; } = null!;

        [JsonPropertyName("DistrictID")]
        public int DistrictID { get; set; }

        [JsonPropertyName("WardName")]
        public string WardName { get; set; } = null!;
    }

    // =========================================================
    // RESPONSE TRẢ VỀ CHO FRONTEND
    // =========================================================
    public class ShippingFeeCalculationDTO
    {
        public decimal ShippingFee { get; set; }
        public string ExpectedDeliveryTime { get; set; } = "2-3 ngày";
        public string Provider { get; set; } = "GHN";
        public int ServiceTypeId { get; set; } = 2;
        public string ServiceTypeName { get; set; } = "Express";
    }

    // =========================================================
    // PREVIEW SHIPPING CHO CART (trước khi checkout)
    // =========================================================
    public class ShippingPreviewRequest
    {
        public int ToDistrictId { get; set; }
        public string ToWardCode { get; set; } = null!;
        public int ServiceTypeId { get; set; } = 2;
    }

    public class ShippingPreviewResponse
    {
        public List<ShopShippingPreview> ShopShippings { get; set; } = new();
        public decimal TotalShippingFee { get; set; }
    }

    public class ShopShippingPreview
    {
        public int ShopId { get; set; }
        public string ShopName { get; set; } = null!;
        public decimal ShippingFee { get; set; }
        public string ExpectedDeliveryTime { get; set; } = null!;
        public bool IsCalculated { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }
}
