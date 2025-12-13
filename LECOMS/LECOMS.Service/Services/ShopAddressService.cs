using LECOMS.Data.DTOs.Shop;
using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class ShopAddressService : IShopAddressService
    {
        private readonly IUnitOfWork _uow;

        public ShopAddressService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ShopAddressDTO> GetMyShopAddressAsync(string sellerId)
        {
            var shop = await _uow.Shops.GetAsync(s => s.SellerId == sellerId);
            if (shop == null)
                throw new InvalidOperationException("Seller chưa có shop.");

            var address = await _uow.ShopAddresses.GetAsync(
                a => a.ShopId == shop.Id && a.IsDefault);

            if (address == null)
                throw new InvalidOperationException("Shop chưa thiết lập địa chỉ kho.");

            return MapToDTO(address);
        }

        public async Task<ShopAddressDTO> UpsertMyShopAddressAsync(
            string sellerId,
            UpsertShopAddressDTO dto)
        {
            var shop = await _uow.Shops.GetAsync(s => s.SellerId == sellerId);
            if (shop == null)
                throw new InvalidOperationException("Seller chưa có shop.");

            // ❗ Mỗi shop chỉ có 1 địa chỉ default
            var existing = await _uow.ShopAddresses.GetAsync(
                a => a.ShopId == shop.Id && a.IsDefault);

            if (existing != null)
            {
                existing.ProvinceId = dto.ProvinceId;
                existing.ProvinceName = dto.ProvinceName;
                existing.DistrictId = dto.DistrictId;
                existing.DistrictName = dto.DistrictName;
                existing.WardCode = dto.WardCode;
                existing.WardName = dto.WardName;
                existing.DetailAddress = dto.DetailAddress;
                existing.ContactName = dto.ContactName;
                existing.ContactPhone = dto.ContactPhone;
                existing.UpdatedAt = DateTime.UtcNow;

                await _uow.ShopAddresses.UpdateAsync(existing);
                await _uow.CompleteAsync();

                return MapToDTO(existing);
            }

            var address = new ShopAddress
            {
                ShopId = shop.Id,
                ProvinceId = dto.ProvinceId,
                ProvinceName = dto.ProvinceName,
                DistrictId = dto.DistrictId,
                DistrictName = dto.DistrictName,
                WardCode = dto.WardCode,
                WardName = dto.WardName,
                DetailAddress = dto.DetailAddress,
                ContactName = dto.ContactName,
                ContactPhone = dto.ContactPhone,
                IsDefault = true
            };

            await _uow.ShopAddresses.AddAsync(address);
            await _uow.CompleteAsync();

            return MapToDTO(address);
        }

        public async Task<ShopAddressDTO> UpdateMyShopAddressAsync(
    string sellerId,
    int addressId,
    UpsertShopAddressDTO dto)
        {
            var shop = await _uow.Shops.GetAsync(s => s.SellerId == sellerId);
            if (shop == null)
                throw new InvalidOperationException("Seller chưa có shop.");

            var address = await _uow.ShopAddresses.GetAsync(
                a => a.Id == addressId && a.ShopId == shop.Id);

            if (address == null)
                throw new InvalidOperationException("Không tìm thấy địa chỉ kho.");

            // ❗ Nếu set IsDefault = true → bỏ default cũ
            if (dto.IsDefault && !address.IsDefault)
            {
                var currentDefault = await _uow.ShopAddresses.GetAsync(
                    a => a.ShopId == shop.Id && a.IsDefault);

                if (currentDefault != null)
                {
                    currentDefault.IsDefault = false;
                    await _uow.ShopAddresses.UpdateAsync(currentDefault);
                }
            }

            // Update fields
            address.ProvinceId = dto.ProvinceId;
            address.ProvinceName = dto.ProvinceName;
            address.DistrictId = dto.DistrictId;
            address.DistrictName = dto.DistrictName;
            address.WardCode = dto.WardCode;
            address.WardName = dto.WardName;
            address.DetailAddress = dto.DetailAddress;
            address.ContactName = dto.ContactName;
            address.ContactPhone = dto.ContactPhone;
            address.IsDefault = dto.IsDefault;
            address.UpdatedAt = DateTime.UtcNow;

            await _uow.ShopAddresses.UpdateAsync(address);
            await _uow.CompleteAsync();

            return MapToDTO(address);
        }


        private static ShopAddressDTO MapToDTO(ShopAddress a)
        {
            return new ShopAddressDTO
            {
                Id = a.Id,
                ShopId = a.ShopId,
                ProvinceId = a.ProvinceId,
                ProvinceName = a.ProvinceName,
                DistrictId = a.DistrictId,
                DistrictName = a.DistrictName,
                WardCode = a.WardCode,
                WardName = a.WardName,
                DetailAddress = a.DetailAddress,
                IsDefault = a.IsDefault,
                ContactName = a.ContactName,
                ContactPhone = a.ContactPhone
            };
        }
    }
}
