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
    }
}
