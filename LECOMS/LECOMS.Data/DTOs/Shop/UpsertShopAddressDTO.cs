using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Shop
{
    public class UpsertShopAddressDTO
    {
        public int ProvinceId { get; set; }
        public string ProvinceName { get; set; } = null!;

        public int DistrictId { get; set; }
        public string DistrictName { get; set; } = null!;

        public string WardCode { get; set; } = null!;
        public string WardName { get; set; } = null!;

        public string DetailAddress { get; set; } = null!;

        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }

        public bool IsDefault { get; set; } = true;
    }
}
