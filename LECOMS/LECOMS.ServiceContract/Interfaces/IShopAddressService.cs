using LECOMS.Data.DTOs.Shop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IShopAddressService
    {
        Task<ShopAddressDTO> GetMyShopAddressAsync(string sellerId);
        Task<ShopAddressDTO> UpsertMyShopAddressAsync(string sellerId, UpsertShopAddressDTO dto);
        Task<ShopAddressDTO> UpdateMyShopAddressAsync(string sellerId,int addressId,UpsertShopAddressDTO dto);
    }
}
