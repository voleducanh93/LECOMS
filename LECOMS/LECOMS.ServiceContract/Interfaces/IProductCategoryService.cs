using LECOMS.Data.DTOs.Product;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IProductCategoryService
    {
        Task<IEnumerable<ProductCategoryDTO>> GetAllAsync();
        Task<ProductCategoryDTO> CreateAsync(ProductCategoryCreateDTO dto);
    }
}
