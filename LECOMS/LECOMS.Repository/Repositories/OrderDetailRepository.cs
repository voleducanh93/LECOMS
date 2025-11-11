using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    /// <summary>
    /// Repository implementation cho OrderDetail
    /// </summary>
    public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
    {
        public OrderDetailRepository(LecomDbContext db) : base(db) { }

        /// <summary>
        /// Lấy order details theo OrderId ⭐ NEW
        /// </summary>
        public async Task<IEnumerable<OrderDetail>> GetByOrderIdAsync(string orderId)
        {
            return await dbSet
                .Include(od => od.Product)
                    .ThenInclude(p => p.Images)
                .Where(od => od.OrderId == orderId)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy order detail theo OrderId và ProductId ⭐ NEW
        /// </summary>
        public async Task<OrderDetail?> GetByOrderAndProductAsync(string orderId, string productId)
        {
            return await dbSet
                .Include(od => od.Product)
                .FirstOrDefaultAsync(od => od.OrderId == orderId && od.ProductId == productId);
        }

        /// <summary>
        /// Tính tổng quantity của product đã bán ⭐ NEW
        /// Chỉ đếm orders đã thanh toán
        /// </summary>
        public async Task<int> GetTotalQuantitySoldAsync(string productId)
        {
            return await dbSet
                .Include(od => od.Order)
                .Where(od => od.ProductId == productId
                    && od.Order.PaymentStatus == PaymentStatus.Paid)
                .SumAsync(od => od.Quantity);
        }
    }
}