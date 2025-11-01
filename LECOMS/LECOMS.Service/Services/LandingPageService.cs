using AutoMapper;
using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.LandingPage;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class LandingPageService : ILandingPageService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public LandingPageService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<LandingPageDTO> GetLandingPageDataAsync()
        {
            // Lấy dữ liệu thô từ repo
            var allCourses = await _uow.LandingPage.GetAllCoursesAsync();
            var allProducts = await _uow.LandingPage.GetAllProductsAsync();
            var allCourseCats = await _uow.LandingPage.GetAllCourseCategoriesAsync();
            var allProductCats = await _uow.LandingPage.GetAllProductCategoriesAsync();

            // Xử lý logic top 4
            var popularCourses = allCourses
                .Where(c => c.Active == 1)
                .OrderByDescending(c => c.Enrollments.Count)
                .Take(4);

            var bestSellerProducts = allProducts
                .Where(p => p.Status == ProductStatus.Published && p.Active == 1)
                .OrderByDescending(p => p.LastUpdatedAt)
                .Take(4);

            var topCourseCategories = allCourseCats
                .Where(c => c.Active == 1)
                .OrderByDescending(c => c.Courses.Count)
                .Take(4);

            var topProductCategories = allProductCats
                .Where(c => c.Active == 1)
                .OrderByDescending(c => c.Products.Count)
                .Take(4);

            return new LandingPageDTO
            {
                TopCourseCategories = _mapper.Map<IEnumerable<CourseCategoryDTO>>(topCourseCategories),
                TopProductCategories = _mapper.Map<IEnumerable<ProductCategoryDTO>>(topProductCategories),
                PopularCourses = _mapper.Map<IEnumerable<CourseDTO>>(popularCourses),
                BestSellerProducts = _mapper.Map<IEnumerable<ProductDTO>>(bestSellerProducts)
            };
        }
    }
}
