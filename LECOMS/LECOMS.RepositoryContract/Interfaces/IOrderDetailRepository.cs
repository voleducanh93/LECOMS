using LECOMS.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    /// <summary>
    /// Repository interface cho OrderDetail
    /// </summary>
    public interface IOrderDetailRepository : IRepository<OrderDetail>
    {
        /// <summary>
        /// Lấy order details theo OrderId ⭐ NEW
        /// </summary>
        Task<IEnumerable<OrderDetail>> GetByOrderIdAsync(string orderId);

        /// <summary>
        /// Lấy order detail theo OrderId và ProductId ⭐ NEW
        /// </summary>
        Task<OrderDetail?> GetByOrderAndProductAsync(string orderId, string productId);

        /// <summary>
        /// Tính tổng quantity của product đã bán ⭐ NEW
        /// </summary>
        Task<int> GetTotalQuantitySoldAsync(string productId);
    }
}