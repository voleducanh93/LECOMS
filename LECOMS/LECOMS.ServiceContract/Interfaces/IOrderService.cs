using LECOMS.Data.DTOs.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IOrderService
    {
        Task<(OrderDTO Order, string? PaymentUrl)> CreateOrderFromCartAsync(string userId, CheckoutRequestDTO checkout);
        Task<OrderDTO?> GetByIdAsync(string orderId);
        Task<IEnumerable<OrderDTO>> GetByUserAsync(string userId);
    }
}
