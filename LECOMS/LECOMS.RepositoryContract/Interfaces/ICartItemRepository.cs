using LECOMS.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    /// <summary>
    /// Repository interface cho CartItem
    /// </summary>
    public interface ICartItemRepository : IRepository<CartItem>
    {
        /// <summary>
        /// Lấy cart item theo CartId và ProductId
        /// </summary>
        Task<CartItem?> GetByCartAndProductAsync(string cartId, string productId);

        /// <summary>
        /// Lấy tất cả items của cart
        /// </summary>
        Task<IEnumerable<CartItem>> GetByCartIdAsync(string cartId);

        /// <summary>
        /// Xóa tất cả items của cart
        /// </summary>
        Task DeleteAllByCartIdAsync(string cartId);
    }
}