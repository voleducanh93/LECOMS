﻿using LECOMS.Data.DTOs.Seller;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IShopService
    {
        Task<ShopDTO> CreateShopAsync(string sellerId, SellerRegistrationRequestDTO dto);
        Task<ShopDTO> GetShopBySellerIdAsync(string sellerId);
        Task<IEnumerable<ShopDTO>> GetAllAsync(string? status = null);
        Task<ShopDTO> GetByIdAsync(int id); // 👈 thêm dòng này
        Task<ShopDTO> UpdateShopAsync(int id, ShopUpdateDTO dto, string userId, bool isAdmin);
        Task<bool> DeleteShopAsync(int id, string userId, bool isAdmin);
        Task<ShopDTO> ApproveShopAsync(int id, string adminId);
        Task<ShopDTO> RejectShopAsync(int id, string adminId, string reason);
        Task<bool> HasShopAsync(string sellerId);

    }
}
