using LECOMS.Data.DTOs.Order;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    /// <summary>
    /// Service xử lý Orders
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Tạo order từ cart và tạo payment link PayOS
        /// 
        /// FLOW:
        /// 1. Validate cart (không rỗng, đủ stock)
        /// 2. Group cart items theo shop (1 cart có thể có nhiều shops)
        /// 3. Tạo Order cho TỪNG SHOP
        /// 4. Tạo OrderDetails
        /// 5. Giảm stock
        /// 6. Tạo Payment link PayOS
        /// 7. Clear cart
        /// 8. Return orders + payment URL
        /// </summary>
        /// <returns>List of orders + payment URL</returns>
        Task<CheckoutResultDTO> CreateOrderFromCartAsync(string userId, CheckoutRequestDTO checkout);

        /// <summary>
        /// Lấy order by ID
        /// </summary>
        Task<OrderDTO?> GetByIdAsync(string orderId);

        /// <summary>
        /// Lấy orders của user
        /// </summary>
        Task<IEnumerable<OrderDTO>> GetByUserAsync(string userId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Lấy orders của shop
        /// </summary>
        Task<IEnumerable<OrderDTO>> GetByShopAsync(int shopId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Update order status (admin/shop)
        /// </summary>
        Task<OrderDTO> UpdateOrderStatusAsync(string orderId, string status, string userId);

        /// <summary>
        /// Customer xác nhận đã nhận hàng
        /// Trigger release balance sau holding period
        /// </summary>
        Task<OrderDTO> ConfirmReceivedAsync(string orderId, string userId);
    }
}