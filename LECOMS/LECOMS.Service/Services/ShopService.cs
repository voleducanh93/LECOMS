using AutoMapper;
using LECOMS.Data.DTOs.Seller;
using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class ShopService : IShopService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public ShopService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
        public async Task<ShopDTO> GetByIdAsync(int id)
        {
            var shop = await _uow.Shops.GetAsync(s => s.Id == id, includeProperties: "Category");
            return shop == null ? null : _mapper.Map<ShopDTO>(shop);
        }
        public async Task<bool> HasShopAsync(string sellerId)
        {
            return await _uow.Shops.ExistsBySellerIdAsync(sellerId);
        }
        public async Task<ShopDTO> CreateShopAsync(string sellerId, SellerRegistrationRequestDTO dto)
        {
            if (await _uow.Shops.ExistsBySellerIdAsync(sellerId))
                throw new InvalidOperationException("This seller already has a shop.");

            // ✅ Kiểm tra category tồn tại
            var category = await _uow.CourseCategories.GetAsync(c => c.Id == dto.CategoryId);
            if (category == null)
                throw new InvalidOperationException("Selected category not found.");

            var shop = new Shop
            {
                Name = dto.ShopName,
                Description = dto.ShopDescription,
                PhoneNumber = dto.ShopPhoneNumber,
                Address = dto.ShopAddress,
                BusinessType = dto.BusinessType,
                OwnershipDocumentUrl = dto.OwnershipDocumentUrl,
                CategoryId = dto.CategoryId,
                AcceptedTerms = dto.AcceptedTerms,
                OwnerFullName = dto.OwnerFullName,
                OwnerDateOfBirth = dto.OwnerDateOfBirth,
                OwnerPersonalIdNumber = dto.OwnerPersonalIdNumber,
                OwnerPersonalIdFrontUrl = dto.OwnerPersonalIdFrontUrl,
                OwnerPersonalIdBackUrl = dto.OwnerPersonalIdBackUrl,
                SellerId = sellerId,
                Status = "Pending"
            };

            await _uow.Shops.AddAsync(shop);
            await _uow.CompleteAsync();
            shop = await _uow.Shops.GetAsync(s => s.Id == shop.Id, includeProperties: "Category");

            return _mapper.Map<ShopDTO>(shop);
        }


        public async Task<ShopDTO> GetShopBySellerIdAsync(string sellerId)
        {
            var shop = await _uow.Shops.GetBySellerIdAsync(sellerId, includeProperties: "Category");
            return shop == null ? null : _mapper.Map<ShopDTO>(shop);
        }

        public async Task<IEnumerable<ShopDTO>> GetAllAsync(string? status = null)
        {
            var shops = await _uow.Shops.GetAllAsync(
                filter: s => string.IsNullOrEmpty(status) || s.Status == status,
                includeProperties: "Category"
            );
            return _mapper.Map<IEnumerable<ShopDTO>>(shops);
        }

        public async Task<ShopDTO> UpdateShopAsync(int id, ShopUpdateDTO dto, string userId, bool isAdmin)
        {
            var shop = await _uow.Shops.GetAsync(s => s.Id == id);
            if (shop == null) throw new KeyNotFoundException("Shop not found.");

            if (!isAdmin && shop.SellerId != userId)
                throw new UnauthorizedAccessException();

            _mapper.Map(dto, shop);
            await _uow.Shops.UpdateAsync(shop);
            await _uow.CompleteAsync();
            shop = await _uow.Shops.GetAsync(s => s.Id == id, includeProperties: "Category");

            return _mapper.Map<ShopDTO>(shop);
        }

        public async Task<bool> DeleteShopAsync(int id, string userId, bool isAdmin)
        {
            var shop = await _uow.Shops.GetAsync(s => s.Id == id);
            if (shop == null) return false;

            if (!isAdmin && shop.SellerId != userId)
                throw new UnauthorizedAccessException();

            await _uow.Shops.DeleteAsync(shop);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<ShopDTO> ApproveShopAsync(int id, string adminId)
        {
            var shop = await _uow.Shops.GetAsync(s => s.Id == id);
            if (shop == null) throw new KeyNotFoundException("Shop not found.");

            shop.Status = "Approved";
            shop.ApprovedAt = DateTime.UtcNow;

            await _uow.Shops.UpdateAsync(shop);
            await _uow.CompleteAsync();

            shop = await _uow.Shops.GetAsync(s => s.Id == id, includeProperties: "Category");
            return _mapper.Map<ShopDTO>(shop);
        }

        public async Task<ShopDTO> RejectShopAsync(int id, string adminId, string reason)
        {
            var shop = await _uow.Shops.GetAsync(s => s.Id == id);
            if (shop == null) throw new KeyNotFoundException("Shop not found.");

            shop.Status = "Rejected";
            shop.RejectedReason = reason;

            await _uow.Shops.UpdateAsync(shop);
            await _uow.CompleteAsync();
            shop = await _uow.Shops.GetAsync(s => s.Id == id, includeProperties: "Category");

            return _mapper.Map<ShopDTO>(shop);
        }
    }
}
