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
        /// </summary>
        public async Task<IEnumerable<ProductDTO>> GetAllByShopAsync(int shopId)
        {
            var shop = await _uow.Shops.GetAsync(s => s.Id == shopId);
            if (shop == null)
                throw new InvalidOperationException("Shop not found.");

            var products = await _uow.Products.GetAllByShopAsync(shopId, includeProperties: "Category,Images");
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
                throw new KeyNotFoundException("Product not found.");

            return _mapper.Map<ProductDTO>(product);
        }

        /// <summary>
        /// Seller tạo sản phẩm mới (kèm nhiều ảnh)
        /// </summary>
        public async Task<ProductDTO> CreateAsync(int shopId, ProductCreateDTO dto)
        {
            using var transaction = await _uow.BeginTransactionAsync();

            try
            {
                // ✅ Kiểm tra Shop và Category hợp lệ
                var shop = await _uow.Shops.GetAsync(s => s.Id == shopId)
                           ?? throw new InvalidOperationException("Shop not found.");

                var category = await _uow.ProductCategories.GetAsync(c => c.Id == dto.CategoryId)
                               ?? throw new InvalidOperationException("Category not found.");

                // ✅ Sinh slug
                var slug = GenerateSlug(dto.Name);
                if (await _uow.Products.ExistsSlugAsync(slug))
                    throw new InvalidOperationException("Slug already exists for another product.");

                // ✅ Tạo Product entity
                var product = _mapper.Map<Product>(dto);
                product.Id = Guid.NewGuid().ToString();
                product.Slug = slug;
                product.ShopId = shopId; // ✅ Gán ShopId tại đây
                product.LastUpdatedAt = DateTime.UtcNow;
                product.Status = dto.Status ?? Data.Enum.ProductStatus.Draft;

                await _uow.Products.AddAsync(product);

                // ✅ Nếu có ảnh -> lưu ProductImage
                if (dto.Images != null && dto.Images.Count > 0)
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
        /// Cập nhật thông tin sản phẩm (có thể thay thế toàn bộ ảnh)
        /// </summary>
        public async Task<ProductDTO> UpdateAsync(string id, ProductUpdateDTO dto)
        {
            var product = await _uow.Products.GetAsync(p => p.Id == id, includeProperties: "Images");
            if (product == null) throw new KeyNotFoundException("Product not found.");

            // ✅ Cập nhật dữ liệu cơ bản
            if (!string.IsNullOrEmpty(dto.Name))
            {
                product.Name = dto.Name.Trim();
                product.Slug = GenerateSlug(dto.Name);
            }
            if (!string.IsNullOrEmpty(dto.Description)) product.Description = dto.Description;
            if (!string.IsNullOrEmpty(dto.CategoryId)) product.CategoryId = dto.CategoryId;
            if (dto.Price.HasValue) product.Price = dto.Price.Value;
            if (dto.Stock.HasValue) product.Stock = dto.Stock.Value;
            if (dto.Status.HasValue) product.Status = dto.Status.Value;

            product.LastUpdatedAt = DateTime.UtcNow;

            // ✅ Nếu có danh sách ảnh mới, xoá ảnh cũ trước
            if (dto.Images != null)
            {
                await _uow.ProductImages.DeleteAllByProductIdAsync(product.Id);
                await _uow.CompleteAsync(); // đảm bảo xoá thành công trước khi thêm mới

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
            var product = await _uow.Products.GetAsync(p => p.Id == id, includeProperties: "Images");
            if (product == null) return false;

            await _uow.ProductImages.DeleteAllByProductIdAsync(product.Id);
            await _uow.Products.DeleteAsync(product);

            await _uow.CompleteAsync();
            return true;
        }

        // Helper: sinh slug SEO-friendly
        private static string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.ToLowerInvariant();
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]+", "-").Trim('-');
            return s;
        }
        /// <summary>
        /// Lấy danh sách sản phẩm public (cho trang search + homepage)
        /// Hỗ trợ: search, filter, sort, pagination
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
            // Base query
            IQueryable<Product> query = _uow.Products.Query()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Shop)
                .Where(p => p.Active == 1 && p.Status == ProductStatus.Published);

            // 🔍 Search theo tên sản phẩm, mô tả, hoặc shop name
            if (!string.IsNullOrWhiteSpace(search))
            {
                string lower = search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(lower) ||
                    (p.Description != null && p.Description.ToLower().Contains(lower)) ||
                    (p.Shop != null && p.Shop.Name.ToLower().Contains(lower))
                );
            }

            // 🏷️ Lọc theo category slug
            if (!string.IsNullOrEmpty(category))
            {
                var cat = await _uow.ProductCategories.GetAsync(c => c.Slug == category && c.Active == 1);
                if (cat != null)
                    query = query.Where(p => p.CategoryId == cat.Id);
            }

            // 💰 Lọc theo khoảng giá
            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // 🔽 Sort
            query = sort?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "oldest" => query.OrderBy(p => p.LastUpdatedAt),
                _ => query.OrderByDescending(p => p.LastUpdatedAt) // default newest
            };

            // 📄 Pagination
            int totalItems = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ✅ Map sang DTO
            var items = _mapper.Map<IEnumerable<ProductDTO>>(products);

            // ✅ Trả về object chứa meta-data
            return new
            {
                totalItems,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                items
            };
        }
        public async Task<ProductDTO> GetBySlugAsync(string slug)
        {
            var product = await _uow.Products.GetAsync(
                p => p.Slug == slug && p.Active == 1,
                includeProperties: "Category,Images,Shop"
            );

            if (product == null)
                throw new KeyNotFoundException("Product not found.");

            return _mapper.Map<ProductDTO>(product);
        }

    }
}
