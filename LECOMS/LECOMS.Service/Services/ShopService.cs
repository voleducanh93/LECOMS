using AutoMapper;
using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.DTOs.Seller;
using LECOMS.Data.DTOs.Shop;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
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
            // Không dùng nữa nếu chọn Option 2, nhưng giữ lại nếu chỗ khác cần
            return await _uow.Shops.ExistsBySellerIdAsync(sellerId);
        }

        // ----------------------------------------------------------------------
        // CREATE SHOP (Customer đăng ký)
        // ----------------------------------------------------------------------
        public async Task<ShopDTO> CreateShopAsync(string sellerId, SellerRegistrationRequestDTO dto)
        {
            // Lấy shop hiện có của seller (nếu có)
            var existing = await _uow.Shops.GetAsync(s => s.SellerId == sellerId);

            if (existing != null)
            {
                // Nếu shop đang Pending hoặc Approved → KHÔNG cho đăng ký lại
                if (existing.Status == "Pending" || existing.Status == "Approved")
                {
                    throw new InvalidOperationException("Bạn đã có cửa hàng đang hoạt động hoặc chờ duyệt.");
                }

                // Nếu shop đang Rejected → UPDATE lại hồ sơ, reset về Pending
                if (existing.Status == "Rejected")
                {
                    existing.Name = dto.ShopName;
                    existing.Description = dto.ShopDescription;
                    existing.PhoneNumber = dto.ShopPhoneNumber;
                    existing.Address = dto.ShopAddress;
                    existing.BusinessType = dto.BusinessType;
                    existing.OwnershipDocumentUrl = dto.OwnershipDocumentUrl;
                    existing.ShopAvatar = dto.ShopAvatar;
                    existing.ShopBanner = dto.ShopBanner;
                    existing.ShopFacebook = dto.ShopFacebook;
                    existing.ShopTiktok = dto.ShopTiktok;
                    existing.ShopInstagram = dto.ShopInstagram;
                    existing.CategoryId = dto.CategoryId;
                    existing.AcceptedTerms = dto.AcceptedTerms;
                    existing.OwnerFullName = dto.OwnerFullName;
                    existing.OwnerDateOfBirth = dto.OwnerDateOfBirth;
                    existing.OwnerPersonalIdNumber = dto.OwnerPersonalIdNumber;
                    existing.OwnerPersonalIdFrontUrl = dto.OwnerPersonalIdFrontUrl;
                    existing.OwnerPersonalIdBackUrl = dto.OwnerPersonalIdBackUrl;

                    existing.Status = "Pending";
                    existing.RejectedReason = null;   // reset lý do cũ

                    await _uow.Shops.UpdateAsync(existing);
                    await _uow.CompleteAsync();

                    var updated = await _uow.Shops.GetAsync(
                        s => s.Id == existing.Id,
                        includeProperties: "Category"
                    );

                    return _mapper.Map<ShopDTO>(updated);
                }
            }

            // === Không có shop cũ → tạo shop mới như bình thường ===

            // Kiểm tra category tồn tại
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

            // Tạo ShopWallet mới
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


        // ----------------------------------------------------------------------
        // GET SHOP BY SELLER
        // ----------------------------------------------------------------------
        public async Task<ShopDTO> GetShopBySellerIdAsync(string sellerId)
        {
            var shop = await _uow.Shops.GetBySellerIdAsync(sellerId, includeProperties: "Category");
            return shop == null ? null : _mapper.Map<ShopDTO>(shop);
        }

        // ----------------------------------------------------------------------
        // ADMIN: GET ALL
        // ----------------------------------------------------------------------
        public async Task<IEnumerable<ShopDTO>> GetAllAsync(string? status = null)
        {
            var shops = await _uow.Shops.GetAllAsync(
                filter: s => string.IsNullOrEmpty(status) || s.Status == status,
                includeProperties: "Category"
            );
            return _mapper.Map<IEnumerable<ShopDTO>>(shops);
        }

        // ----------------------------------------------------------------------
        // UPDATE SHOP (seller or admin)
        // ----------------------------------------------------------------------
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

        // ----------------------------------------------------------------------
        // DELETE SHOP (seller tự xoá shop)
        // ----------------------------------------------------------------------
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

        // ----------------------------------------------------------------------
        // ADMIN APPROVE SHOP
        // ----------------------------------------------------------------------
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

        // ----------------------------------------------------------------------
        // ADMIN REJECT SHOP (OPTION 2 → XÓA SHOP HOÀN TOÀN)
        // ----------------------------------------------------------------------
        public async Task<ShopDTO> RejectShopAsync(int id, string adminId, string reason)
        {
            var shop = await _uow.Shops.GetAsync(s => s.Id == id, includeProperties: "Category");
            if (shop == null)
                throw new KeyNotFoundException("Không tìm thấy cửa hàng.");

            shop.Status = "Rejected";
            shop.RejectedReason = reason;

            await _uow.Shops.UpdateAsync(shop);
            await _uow.CompleteAsync();

            return _mapper.Map<ShopDTO>(shop);
        }


        // ----------------------------------------------------------------------
        // PUBLIC: GET SHOP DETAIL
        // ----------------------------------------------------------------------
        public async Task<object> GetPublicShopDetailAsync(int shopId)
        {
            var shop = await _uow.Shops.GetAsync(
                s => s.Id == shopId && s.Status.ToLower() == ShopStatus.Approved.ToString().ToLower(),
                includeProperties: "Category"
            );

            if (shop == null)
                throw new KeyNotFoundException("Không tìm thấy cửa hàng.");

            var products = await _uow.Products.GetAllAsync(
                p => p.ShopId == shopId && p.Active == 1,
                includeProperties: "Category,Images"
            );

            var courses = await _uow.Courses.GetAllAsync(
                c => c.ShopId == shopId && c.Active == 1,
                includeProperties: "Category"
            );

            return new
            {
                shop = _mapper.Map<ShopDTO>(shop),
                products = _mapper.Map<IEnumerable<ProductDTO>>(products),
                courses = _mapper.Map<IEnumerable<CourseDTO>>(courses)
            };
        }

        public async Task ConnectGHNAsync(string sellerId, ConnectGHNRequestDTO dto)
        {
            var shop = await _uow.Shops.GetAsync(s => s.SellerId == sellerId)
                ?? throw new InvalidOperationException("Shop không tồn tại.");

            // Test GHN token + shopId
            using var client = new HttpClient
            {
                BaseAddress = new Uri("https://online-gateway.ghn.vn/shiip/public-api/")
            };

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Token", dto.GHNToken.Trim());
            client.DefaultRequestHeaders.Add("ShopId", dto.GHNShopId.Trim());

            var testRequest = new
            {
                from_district_id = 1442,
                to_district_id = 1442,
                weight = 500,
                service_type_id = 2
            };

            var res = await client.PostAsJsonAsync(
                "v2/shipping-order/fee",
                testRequest);

            if (!res.IsSuccessStatusCode)
            {
                var raw = await res.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"GHN Token hoặc ShopId không hợp lệ: {raw}");
            }

            shop.GHNToken = dto.GHNToken.Trim();
            shop.GHNShopId = dto.GHNShopId.Trim();

            await _uow.Shops.UpdateAsync(shop);
            await _uow.CompleteAsync();
        }

    }
}
    