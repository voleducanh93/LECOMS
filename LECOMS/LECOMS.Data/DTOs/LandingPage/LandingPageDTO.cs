using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.LandingPage
{
    public class LandingPageDTO
    {
        public IEnumerable<CourseCategoryDTO> TopCourseCategories { get; set; } = new List<CourseCategoryDTO>();
        public IEnumerable<ProductCategoryDTO> TopProductCategories { get; set; } = new List<ProductCategoryDTO>();
        public IEnumerable<CourseDTO> PopularCourses { get; set; } = new List<CourseDTO>();
        public IEnumerable<ProductDTO> BestSellerProducts { get; set; } = new List<ProductDTO>();
    }
}
