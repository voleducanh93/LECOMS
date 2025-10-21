using AutoMapper;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public ProductCategoryService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProductCategoryDTO>> GetAllAsync()
        {
            var list = await _uow.ProductCategories.GetAllAsync();
            return list.Select(c => _mapper.Map<ProductCategoryDTO>(c));
        }

        public async Task<ProductCategoryDTO> CreateAsync(ProductCategoryCreateDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required");

            var name = dto.Name.Trim();
            var slug = ToSlug(name);

            // Kiểm tra trùng Slug
            var existBySlug = await _uow.ProductCategories.GetAsync(c => c.Slug == slug);
            if (existBySlug != null)
                throw new InvalidOperationException("Slug already exists for another category.");

            // Kiểm tra trùng tên
            var existByName = await _uow.ProductCategories.GetByNameAsync(name);
            if (existByName != null)
                throw new InvalidOperationException("Category name already exists.");

            var category = new ProductCategory
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Slug = slug,
                Description = dto.Description,
                Active = 1
            };

            await _uow.ProductCategories.AddAsync(category);
            await _uow.CompleteAsync();

            return _mapper.Map<ProductCategoryDTO>(category);
        }

        // Helper: Tạo slug
        private static string ToSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var s = input.ToLowerInvariant();
            s = s.Normalize(System.Text.NormalizationForm.FormD);
            var chars = s.Where(c =>
                System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark
            ).ToArray();
            s = new string(chars).Normalize(System.Text.NormalizationForm.FormC);
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]+", "-").Trim('-');
            s = System.Text.RegularExpressions.Regex.Replace(s, "-{2,}", "-");

            return s;
        }
    }
}
