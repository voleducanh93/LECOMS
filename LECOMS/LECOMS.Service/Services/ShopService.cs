using AutoMapper;
using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.DTOs.Seller;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
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
                throw new InvalidOperationException("Người bán này đã có một cửa hàng.");

            // ✅ Kiểm tra category tồn tại
            var category = await _uow.CourseCategories.GetAsync(c => c.Id == dto.CategoryId);
            if (category == null)
                throw new InvalidOperationException("Không tìm thấy danh mục đã chọn.");

            var shop = new Shop
            {
                Name = dto.ShopName,
                Description = dto.ShopDescription,
                PhoneNumber = dto.ShopPhoneNumber,
                Address = dto.ShopAddress,
                BusinessType = dto.BusinessType,
                OwnershipDocumentUrl = dto.OwnershipDocumentUrl,
                ShopAvatar = dto.ShopAvatar,
                ShopBanner = dto.ShopBanner,
                ShopFacebook = dto.ShopFacebook,
                ShopTiktok = dto.ShopTiktok,
                ShopInstagram = dto.ShopInstagram,
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

            var wallet = new ShopWallet
            {
                Id = Guid.NewGuid().ToString(),
                ShopId = shop.Id,
                AvailableBalance = 0,
                PendingBalance = 0,
                TotalEarned = 0,
                TotalWithdrawn = 0,
                TotalRefunded = 0,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            await _uow.ShopWallets.AddAsync(wallet);
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
            if (shop == null) throw new KeyNotFoundException("Không tìm thấy cửa hàng.");

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
            if (shop == null) throw new KeyNotFoundException("Không tìm thấy cửa hàng.");

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
            if (shop == null) throw new KeyNotFoundException("Không tìm thấy cửa hàng.");

            shop.Status = "Rejected";
            shop.RejectedReason = reason;

            await _uow.Shops.UpdateAsync(shop);
            await _uow.CompleteAsync();
            shop = await _uow.Shops.GetAsync(s => s.Id == id, includeProperties: "Category");

            return _mapper.Map<ShopDTO>(shop);
        }
        public async Task<object> GetPublicShopDetailAsync(int shopId)
        {
            var shop = await _uow.Shops.GetAsync(
                s => s.Id == shopId && s.Status.ToLower() == ShopStatus.Approved.ToString().ToLower(),
                includeProperties: "Category"
            ); 
            if (shop == null)
                throw new KeyNotFoundException("Không tìm thấy cửa hàng.");

            // Lấy sản phẩm của shop
            var products = await _uow.Products.GetAllAsync(
                p => p.ShopId == shopId && p.Active == 1,
                includeProperties: "Category,Images"
            );

            // Lấy courses của shop
            var courses = await _uow.Courses.GetAllAsync(
                c => c.ShopId == shopId && c.Active == 1,
                includeProperties: "Category"
            );

            var productDtos = _mapper.Map<IEnumerable<ProductDTO>>(products);
            var courseDtos = _mapper.Map<IEnumerable<CourseDTO>>(courses);
            var shopDto = _mapper.Map<ShopDTO>(shop);

            return new
            {
                shop = shopDto,
                products = productDtos,
                courses = courseDtos
            };
        }

    }
}
