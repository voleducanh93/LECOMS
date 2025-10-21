using AutoMapper;
using LECOMS.Data.DTOs.Product;
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
        /// Lấy tất cả sản phẩm thuộc shop
        /// </summary>
        public async Task<IEnumerable<ProductDTO>> GetAllByShopAsync(int shopId)
        {
            var shop = await _uow.Shops.GetAsync(s => s.Id == shopId);
            if (shop == null)
                throw new InvalidOperationException("Shop not found.");

            var products = await _uow.Products.GetAllByShopAsync(shopId, includeProperties: "Category");
            return _mapper.Map<IEnumerable<ProductDTO>>(products);
        }

        /// <summary>
        /// Lấy sản phẩm theo ID
        /// </summary>
        public async Task<ProductDTO> GetByIdAsync(string id)
        {
            var product = await _uow.Products.GetAsync(p => p.Id == id, includeProperties: "Category");
            if (product == null)
                throw new KeyNotFoundException("Product not found.");

            return _mapper.Map<ProductDTO>(product);
        }

        /// <summary>
        /// Seller tạo sản phẩm mới
        /// </summary>
        public async Task<ProductDTO> CreateAsync(int shopId, ProductCreateDTO dto)
        {
            var shop = await _uow.Shops.GetAsync(s => s.Id == shopId);
            if (shop == null)
                throw new InvalidOperationException("Shop not found.");

            var category = await _uow.ProductCategories.GetAsync(c => c.Id == dto.CategoryId);
            if (category == null)
                throw new InvalidOperationException("Category not found.");

            var slug = GenerateSlug(dto.Name);
            var existSlug = await _uow.Products.ExistsSlugAsync(slug);
            if (existSlug)
                throw new InvalidOperationException("Slug already exists for another product.");

            var product = new Product
            {
                Id = Guid.NewGuid().ToString(),
                Name = dto.Name.Trim(),
                Slug = slug,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                Price = dto.Price,
                Stock = dto.Stock,
                Active = 1
            };

            await _uow.Products.AddAsync(product);
            await _uow.CompleteAsync();

            product = await _uow.Products.GetAsync(p => p.Id == product.Id, includeProperties: "Category");
            return _mapper.Map<ProductDTO>(product);
        }

        /// <summary>
        /// Cập nhật sản phẩm
        /// </summary>
        public async Task<ProductDTO> UpdateAsync(string id, ProductUpdateDTO dto)
        {
            var product = await _uow.Products.GetAsync(p => p.Id == id);
            if (product == null)
                throw new KeyNotFoundException("Product not found.");

            if (!string.IsNullOrEmpty(dto.Name))
            {
                product.Name = dto.Name.Trim();
                product.Slug = GenerateSlug(dto.Name);
            }
            if (!string.IsNullOrEmpty(dto.Description))
                product.Description = dto.Description;

            if (!string.IsNullOrEmpty(dto.CategoryId))
                product.CategoryId = dto.CategoryId;

            if (dto.Price.HasValue)
                product.Price = dto.Price.Value;

            if (dto.Stock.HasValue)
                product.Stock = dto.Stock.Value;

            await _uow.Products.UpdateAsync(product);
            await _uow.CompleteAsync();

            product = await _uow.Products.GetAsync(p => p.Id == id, includeProperties: "Category");
            return _mapper.Map<ProductDTO>(product);
        }

        /// <summary>
        /// Xoá sản phẩm
        /// </summary>
        public async Task<bool> DeleteAsync(string id)
        {
            var product = await _uow.Products.GetAsync(p => p.Id == id);
            if (product == null) return false;

            await _uow.Products.DeleteAsync(product);
            await _uow.CompleteAsync();
            return true;
        }

        // Helper: sinh slug
        private static string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.ToLowerInvariant();
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]+", "-").Trim('-');
            return s;
        }
    }
}
