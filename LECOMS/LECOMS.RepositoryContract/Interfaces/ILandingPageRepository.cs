using LECOMS.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface ILandingPageRepository
    {
        Task<IEnumerable<Course>> GetAllCoursesAsync();
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<IEnumerable<CourseCategory>> GetAllCourseCategoriesAsync();
        Task<IEnumerable<ProductCategory>> GetAllProductCategoriesAsync();
    }
}
