using LECOMS.Data.DTOs.Product;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDTO>> GetAllByShopAsync(int shopId);
        Task<ProductDTO> GetByIdAsync(string id);
        Task<ProductDTO> CreateAsync(int shopId, ProductCreateDTO dto);
        Task<ProductDTO> UpdateAsync(string id, ProductUpdateDTO dto);
        Task<bool> DeleteAsync(string id);
        Task<object> GetPublicProductsAsync(
    string? search = null,
    string? category = null,
    string? sort = null,
    int page = 1,
    int pageSize = 10,
    decimal? minPrice = null,
    decimal? maxPrice = null
);

    }
}
