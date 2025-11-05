using AutoMapper;
using LECOMS.Data.DTOs.Course;
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
    public class CourseCategoryService : ICourseCategoryService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public CourseCategoryService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CourseCategoryDTO>> GetAllAsync()
        {
            var list = await _uow.CourseCategories.GetAllAsync();
            return list.Select(c => _mapper.Map<CourseCategoryDTO>(c));
        }

        public async Task<CourseCategoryDTO> CreateAsync(CourseCategoryCreateDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required");

            var name = dto.Name.Trim();
            var slug = ToSlug(name);

            // 1) Check trùng theo Slug (đúng với Unique Index hiện có)
            var existBySlug = await _uow.CourseCategories.GetAsync(c => c.Slug == slug);
            if (existBySlug != null)
                throw new InvalidOperationException("Slug already exists for another category.");

            // 2) (Tuỳ chọn) Check thêm theo Name để thân thiện với người dùng
            var existByName = await _uow.CourseCategories.GetByNameAsync(name);
            if (existByName != null)
                throw new InvalidOperationException("Category name already exists.");

            var category = new CourseCategory
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Slug = slug,
                Description = dto.Description,
                Active = 1
            };

            await _uow.CourseCategories.AddAsync(category);
            await _uow.CompleteAsync();

            return _mapper.Map<CourseCategoryDTO>(category);
        }

        // Helper: sinh slug từ name
        private static string ToSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var s = input.ToLowerInvariant();

            // bỏ dấu tiếng Việt
            s = s.Normalize(System.Text.NormalizationForm.FormD);
            var chars = s.Where(c =>
                System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark
            ).ToArray();
            s = new string(chars).Normalize(System.Text.NormalizationForm.FormC);

            // thay ký tự không phải a-z0-9 bằng '-'
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]+", "-").Trim('-');
            // rút gọn dấu '-'
            s = System.Text.RegularExpressions.Regex.Replace(s, "-{2,}", "-");

            return s;
        }

    }
}
