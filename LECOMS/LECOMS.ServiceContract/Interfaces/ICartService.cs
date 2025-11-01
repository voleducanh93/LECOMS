using LECOMS.Data.DTOs.Cart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface ICartService
    {
        Task<CartDTO> GetCartAsync(string userId);
        Task<CartDTO> AddItemAsync(string userId, string productId, int quantity);
        Task<CartDTO> RemoveItemAsync(string userId, string productId);
        Task<bool> ClearCartAsync(string userId);
    }
}
