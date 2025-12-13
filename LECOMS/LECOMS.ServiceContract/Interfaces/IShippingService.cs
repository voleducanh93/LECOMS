using LECOMS.Data.DTOs.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    /// <summary>
    /// Service tính phí ship từ GHN API
    /// </summary>
    public interface IShippingService
    {
        Task<decimal> CalculateShippingFeeAsync(
            string ghnToken,
            string ghnShopId,
            int fromDistrictId,
            string fromWardCode,
            int toDistrictId,
            string toWardCode,
            int weight,
            decimal orderValue,
            int serviceTypeId = 2);

        Task<ShippingFeeCalculationDTO?> GetShippingDetailsAsync(
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
            int? height = null);
    }

}
