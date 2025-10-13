using LECOMS.Data.Entities;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface IShopRepository : IRepository<Shop>
    {
        Task<Shop> GetBySellerIdAsync(string sellerId, string? includeProperties = null);
        Task<bool> ExistsBySellerIdAsync(string sellerId);
        Task<Shop> UpdateShopAsync(Shop entity);
    }
}
