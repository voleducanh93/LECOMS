using AutoMapper;
using LECOMS.Data.DTOs.Product;
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
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public ProductService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        /// <summary>
        /// Lấy tất cả sản phẩm thuộc shop (bao gồm Category và Images)
        /// Seller nhìn được cả pending/rejected
        /// </summary>
        public async Task<IEnumerable<ProductDTO>> GetAllByShopAsync(int shopId)
        {
            var shop = await _uow.Shops.GetAsync(s => s.Id == shopId);
            if (shop == null)
                throw new InvalidOperationException("Không tìm thấy cửa hàng.");

            var products = await _uow.Products.Query()
                .Where(p => p.ShopId == shopId)   // ⭐ KHÔNG LỌC Active/Status
                .Include(p => p.Category)
                .Include(p => p.Images)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductDTO>>(products);
        }

        /// <summary>
        /// Lấy sản phẩm theo ID (bao gồm Category và Images)
        /// </summary>
        public async Task<ProductDTO> GetByIdAsync(string id)
        {
            var product = await _uow.Products.GetAsync(
                p => p.Id == id,
                includeProperties: "Category,Images,Shop"
            );

            if (product == null)
                throw new KeyNotFoundException("Product không tìm thấy.");

            return _mapper.Map<ProductDTO>(product);

        }

        /// <summary>
        /// Seller tạo sản phẩm mới (pendding duyệt)
        /// </summary>
        public async Task<ProductDTO> CreateAsync(int shopId, ProductCreateDTO dto)
        {
            using var transaction = await _uow.BeginTransactionAsync();
            try
            {
                var shop = await _uow.Shops.GetAsync(s => s.Id == shopId)
                           ?? throw new InvalidOperationException("Không tìm thấy cửa hàng.");

                var category = await _uow.ProductCategories.GetAsync(c => c.Id == dto.CategoryId)
                               ?? throw new InvalidOperationException("Category không tìm thấy.");

                var slug = GenerateSlug(dto.Name);
                if (await _uow.Products.ExistsSlugAsync(slug))
                    throw new InvalidOperationException("Slug already exists.");

                var product = _mapper.Map<Product>(dto);
                product.Id = Guid.NewGuid().ToString();
                product.Slug = slug;
                product.ShopId = shopId;
                product.LastUpdatedAt = DateTime.UtcNow;

                // 🔥 Pending duyệt
                product.Status = ProductStatus.Draft;
                product.ApprovalStatus = ApprovalStatus.Pending;
                product.ModeratorNote = null;

                await _uow.Products.AddAsync(product);

                if (dto.Images != null)
                {
                    foreach (var img in dto.Images)
                    {
                        await _uow.ProductImages.AddAsync(new ProductImage
                        {
                            ProductId = product.Id,
                            Url = img.Url,
                            OrderIndex = img.OrderIndex,
                            IsPrimary = img.IsPrimary
                        });
                    }
                }

                await _uow.CompleteAsync();
                await transaction.CommitAsync();

                var loaded = await _uow.Products.GetAsync(
                    p => p.Id == product.Id,
                    includeProperties: "Category,Images,Shop"
                );

                return _mapper.Map<ProductDTO>(loaded);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Seller update → luôn reset về Pending duyệt
        /// </summary>
        public async Task<ProductDTO> UpdateAsync(string id, ProductUpdateDTO dto)
        {
            var product = await _uow.Products.GetAsync(p => p.Id == id, includeProperties: "Images");
            if (product == null) throw new KeyNotFoundException("Product không tìm thấy.");

            // Update basic fields
            if (!string.IsNullOrEmpty(dto.Name))
            {
                product.Name = dto.Name.Trim();
                product.Slug = GenerateSlug(dto.Name);
            }

            if (!string.IsNullOrEmpty(dto.Description)) product.Description = dto.Description;
            if (!string.IsNullOrEmpty(dto.CategoryId)) product.CategoryId = dto.CategoryId;
            if (dto.Price.HasValue) product.Price = dto.Price.Value;
            if (dto.Stock.HasValue) product.Stock = dto.Stock.Value;

            // 🔥 Nếu seller tự đổi trạng thái → Published
            if (dto.Status.HasValue)
            {
                // Seller muốn publish lại → cần duyệt
                if (dto.Status.Value == ProductStatus.Published)
                {
                    product.Status = ProductStatus.Draft;  // Tạm là Draft
                    product.ApprovalStatus = ApprovalStatus.Pending;
                    product.ModeratorNote = null;
                }
                else
                {
                    // Những trạng thái khác seller đổi trực tiếp
                    product.Status = dto.Status.Value;
                }
            }

            product.LastUpdatedAt = DateTime.UtcNow;

            // 🔥 Handle images
            if (dto.Images != null)
            {
                await _uow.ProductImages.DeleteAllByProductIdAsync(product.Id);
                await _uow.CompleteAsync();

                foreach (var img in dto.Images)
                {
                    await _uow.ProductImages.AddAsync(new ProductImage
                    {
                        ProductId = product.Id,
                        Url = img.Url,
                        OrderIndex = img.OrderIndex,
                        IsPrimary = img.IsPrimary
                    });
                }
            }

            await _uow.Products.UpdateAsync(product);
            await _uow.CompleteAsync();

            // Load lại sau update
            var loaded = await _uow.Products.GetAsync(
                p => p.Id == id,
                includeProperties: "Category,Images"
            );

            return _mapper.Map<ProductDTO>(loaded);
        }


        /// <summary>
        /// Xoá sản phẩm và toàn bộ ảnh liên quan
        /// </summary>
        public async Task<bool> DeleteAsync(string id)
        {
            var product = await _uow.Products.GetAsync(p => p.Id == id);
            if (product == null) return false;

            product.Status = ProductStatus.Archived;
            product.Active = 0; // ẩn khỏi public

            await _uow.Products.UpdateAsync(product);
            await _uow.CompleteAsync();

            return true;
        }


        private static string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.ToLowerInvariant();
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]+", "-").Trim('-');
            return s;
        }

        /// <summary>
        /// Public list → chỉ Approved + Published
        /// </summary>
        public async Task<object> GetPublicProductsAsync(
            string? search = null,
            string? category = null,
            string? sort = null,
            int page = 1,
            int pageSize = 10,
            decimal? minPrice = null,
            decimal? maxPrice = null
        )
        {
            IQueryable<Product> query = _uow.Products.Query()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Shop)
                .Where(p =>
                    p.Active == 1 &&
                    p.Status == ProductStatus.Published &&      // FE muốn xem product đã publish
                    p.ApprovalStatus == ApprovalStatus.Approved // ⭐ moderator đã duyệt
                );

            if (!string.IsNullOrWhiteSpace(search))
            {
                string lower = search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(lower) ||
                    (p.Description != null && p.Description.ToLower().Contains(lower)) ||
                    (p.Shop != null && p.Shop.Name.ToLower().Contains(lower))
                );
            }

            if (!string.IsNullOrEmpty(category))
            {
                var cat = await _uow.ProductCategories.GetAsync(c => c.Slug == category && c.Active == 1);
                if (cat != null)
                    query = query.Where(p => p.CategoryId == cat.Id);
            }

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            query = sort?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "oldest" => query.OrderBy(p => p.LastUpdatedAt),
                _ => query.OrderByDescending(p => p.LastUpdatedAt)
            };

            int totalItems = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = _mapper.Map<IEnumerable<ProductDTO>>(products);

            return new
            {
                totalItems,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                items
            };
        }

        /// <summary>
        /// Lấy sản phẩm theo slug (chỉ Approved + Published)
        /// </summary>
        public async Task<ProductDTO> GetBySlugAsync(string slug)
        {
            var product = await _uow.Products.GetAsync(
                p => p.Slug == slug
                  && p.Active == 1
                  && p.Status == ProductStatus.Published
                  && p.ApprovalStatus == ApprovalStatus.Approved,
                includeProperties: "Category,Images,Shop"
            );

            if (product == null)
                throw new KeyNotFoundException("Product không tìm thấy.");

            return _mapper.Map<ProductDTO>(product);

        }
        private async Task<int> GetTotalSoldAsync(string productId)
        {
            return await _uow.OrderDetails.Query()
                .Where(od => od.ProductId == productId
                    && od.Order.PaymentStatus == PaymentStatus.Paid
                    && od.Order.Status == OrderStatus.Completed)
                .SumAsync(od => (int?)od.Quantity) ?? 0;
        }
    }
}
