using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface IShopAddressRepository : IRepository<ShopAddress>
    {
        /// <summary>
        /// Lấy địa chỉ mặc định của shop
        /// </summary>
        Task<ShopAddress?> GetDefaultByShopIdAsync(int shopId);

        /// <summary>
        /// Kiểm tra shop đã có địa chỉ chưa
        /// </summary>
        Task<bool> HasAddressAsync(int shopId);
    }
}
