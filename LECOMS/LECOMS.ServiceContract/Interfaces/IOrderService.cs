using LECOMS.Data.DTOs.Order;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    /// <summary>
    /// Service quản lý Orders:
    /// - Checkout
    /// - Payment
    /// - Confirm Delivery
    /// - Query Orders (User / Shop)
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// CHECKOUT FLOW:
        /// 1. Lấy cart và validate
        /// 2. Group sản phẩm theo shop
        /// 3. Tạo Order cho TỪNG SHOP
        /// 4. Tạo OrderDetails
        /// 5. Giảm stock sản phẩm
        /// 
        /// SHIPPING FEE:
        /// - Cố định 30.000 VNĐ cho mỗi shop
        /// 
        /// PAYMENT:
        /// - PAYOS: Tạo PayOS Payment Link
        /// - WALLET: Trừ tiền trực tiếp trong CustomerWallet
        /// 
        /// KẾT QUẢ TRẢ VỀ:
        /// - List orders
        /// - Payment URL (nếu dùng PayOS)
        /// - Breakdown: Shipping, WalletUsed, PayOSAmount, etc.
        /// </summary>
        Task<CheckoutResultDTO> CreateOrderFromCartAsync(string userId, CheckoutRequestDTO checkout);

        /// <summary>
        /// Lấy chi tiết đơn hàng theo OrderId
        /// </summary>
        Task<OrderDTO?> GetByIdAsync(string orderId);

        /// <summary>
        /// Lấy danh sách đơn hàng của user (Customer & Seller đều dùng được)
        /// </summary>
        Task<IEnumerable<OrderDTO>> GetByUserAsync(string userId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Lấy orders của shop theo ShopId
        /// </summary>
        Task<IEnumerable<OrderDTO>> GetByShopAsync(int shopId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Seller cập nhật trạng thái đơn hàng
        /// </summary>
        Task<OrderDTO> UpdateOrderStatusAsync(string orderId, string status, string userId);

        /// <summary>
        /// Customer xác nhận đã nhận hàng
        /// </summary>
        Task<OrderDTO> ConfirmReceivedAsync(string orderId, string userId);

        /// <summary>
        /// Hủy đơn hàng (Customer hoặc Seller)
        /// - Nếu đã thanh toán → hoàn tiền về CustomerWallet
        /// - Hoàn lại stock sản phẩm
        /// </summary>
        Task<OrderDTO> CancelOrderAsync(string orderId, string userId, string cancelReason);
    }
}
