using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    /// <summary>
    /// Repository interface cho Order
    /// </summary>
    public interface IOrderRepository : IRepository<Order>
    {
        /// <summary>
        /// Lấy order with details (OrderDetails, Products, Payments, Shipments)
        /// </summary>
        Task<Order?> GetByIdWithDetailsAsync(string id);

        /// <summary>
        /// Lấy orders theo UserId
        /// </summary>
        Task<IEnumerable<Order>> GetByUserIdAsync(string userId);

        /// <summary>
        /// Lấy orders theo ShopId ⭐ NEW
        /// Dùng cho shop dashboard
        /// </summary>
        Task<IEnumerable<Order>> GetByShopIdAsync(int shopId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Lấy order theo OrderCode ⭐ NEW
        /// OrderCode là unique identifier cho customer
        /// </summary>
        Task<Order?> GetByOrderCodeAsync(string orderCode);

        /// <summary>
        /// Lấy orders cần release balance ⭐ NEW
        /// Dùng cho background job
        /// Điều kiện:
        /// - PaymentStatus = Paid
        /// - Status = Completed
        /// - CompletedAt <= cutoffDate
        /// - BalanceReleased = false
        /// </summary>
        Task<IEnumerable<Order>> GetOrdersToReleaseBalanceAsync(DateTime cutoffDate);

        /// <summary>
        /// Lấy orders theo PaymentStatus ⭐ NEW
        /// </summary>
        Task<IEnumerable<Order>> GetByPaymentStatusAsync(
            PaymentStatus paymentStatus,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Lấy orders theo OrderStatus và ShopId ⭐ NEW
        /// Dùng cho shop filter orders
        /// </summary>
        Task<IEnumerable<Order>> GetByShopAndStatusAsync(
            int shopId,
            OrderStatus status,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Đếm số orders theo điều kiện ⭐ NEW
        /// Override để có typed predicate
        /// </summary>
        Task<int> CountOrdersAsync(Expression<Func<Order, bool>> predicate);

        /// <summary>
        /// Tính tổng doanh thu theo shop trong khoảng thời gian ⭐ NEW
        /// </summary>
        Task<decimal> GetTotalRevenueByShopAsync(int shopId, DateTime fromDate, DateTime toDate);
    }
}