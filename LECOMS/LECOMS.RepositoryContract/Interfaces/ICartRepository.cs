using LECOMS.Data.Entities;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    /// <summary>
    /// Repository interface cho Cart
    /// </summary>
    public interface ICartRepository : IRepository<Cart>
    {
        /// <summary>
        /// Lấy cart theo UserId với optional include properties
        /// </summary>
        Task<Cart?> GetByUserIdAsync(string userId, string? includeProperties = null);

        /// <summary>
        /// Đếm số items trong cart của user ⭐ NEW
        /// </summary>
        Task<int> GetCartItemCountAsync(string userId);

        /// <summary>
        /// Tính tổng giá trị cart của user ⭐ NEW
        /// </summary>
        Task<decimal> GetCartTotalAsync(string userId);

        /// <summary>
        /// Kiểm tra cart có rỗng không ⭐ NEW
        /// </summary>
        Task<bool> IsCartEmptyAsync(string userId);
    }
}