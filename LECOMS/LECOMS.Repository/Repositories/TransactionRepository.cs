using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    /// <summary>
    /// Repository implementation cho Transaction - UPDATED for Marketplace Payment
    /// </summary>
    public class TransactionRepository : Repository<Transaction>, ITransactionRepository
    {
        private readonly LecomDbContext _db;
        public TransactionRepository(LecomDbContext db) : base(db) 
        {
            _db = db;
        }

        /// <summary>
        /// Lấy transaction theo OrderId
        /// ⚠️ NOTE: OrderId có thể chứa nhiều order IDs (comma-separated)
        /// Method này tìm transaction có chứa orderId trong string
        /// </summary>
        public async Task<Transaction?> GetByOrderIdAsync(string orderId)
        {
            // ✅ FIXED: Transaction.OrderId là string có thể chứa nhiều IDs
            // Tìm transaction có chứa orderId này
            return await dbSet
                .FirstOrDefaultAsync(t => t.OrderId.Contains(orderId));
        }

        /// <summary>
        /// Lấy transaction theo PayOS Transaction ID
        /// Dùng khi xử lý webhook từ PayOS
        /// </summary>
        public async Task<Transaction?> GetByPayOSTransactionIdAsync(string payOSTransactionId)
        {
            return await dbSet
                .FirstOrDefaultAsync(t => t.PayOSTransactionId == payOSTransactionId);
        }

        /// <summary>
        /// Lấy transaction theo PayOS Order Code
        /// Dùng trong webhook handler
        /// </summary>
        public async Task<Transaction?> GetByPayOSOrderCodeAsync(long orderCode)
        {
            return await dbSet
                .FirstOrDefaultAsync(t => t.PayOSOrderCode == orderCode);
        }

        /// <summary>
        /// Lấy danh sách transactions theo shop
        /// ⚠️ NOTE: Cần parse OrderId để filter theo shop
        /// </summary>
        public async Task<IEnumerable<Transaction>> GetByShopIdAsync(int shopId, int pageNumber = 1, int pageSize = 20)
        {
            // ✅ APPROACH: Query transactions, sau đó filter bằng code
            // Vì Transaction.OrderId là string (comma-separated), không có FK relationship
            var transactions = await dbSet
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Get all orders của shop này
            var shopOrderIds = await _db.Orders
                .Where(o => o.ShopId == shopId)
                .Select(o => o.Id)
                .ToListAsync();

            // Filter transactions có chứa ít nhất 1 orderId của shop
            var filteredTransactions = transactions
                .Where(t => shopOrderIds.Any(orderId => t.OrderId.Contains(orderId)))
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return filteredTransactions;
        }

        /// <summary>
        /// Lấy danh sách transactions trong khoảng thời gian
        /// </summary>
        public async Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await dbSet
                // ❌ REMOVED: .Include(t => t.Order)
                .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy transactions theo Status
        /// </summary>
        public async Task<IEnumerable<Transaction>> GetByStatusAsync(
            TransactionStatus status,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await dbSet
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Tính tổng platform fee trong khoảng thời gian
        /// Dùng cho revenue report
        /// </summary>
        public async Task<decimal> GetTotalPlatformFeeAsync(DateTime fromDate, DateTime toDate)
        {
            var total = await dbSet
                .Where(t => t.Status == TransactionStatus.Completed
                    && t.CompletedAt.HasValue
                    && t.CompletedAt.Value >= fromDate
                    && t.CompletedAt.Value <= toDate)
                .SumAsync(t => t.PlatformFeeAmount);

            return total;
        }

        /// <summary>
        /// Tính tổng transaction volume
        /// </summary>
        public async Task<decimal> GetTotalVolumeAsync(DateTime fromDate, DateTime toDate)
        {
            return await dbSet
                .Where(t => t.Status == TransactionStatus.Completed
                    && t.CompletedAt.HasValue
                    && t.CompletedAt.Value >= fromDate
                    && t.CompletedAt.Value <= toDate)
                .SumAsync(t => t.TotalAmount);
        }

        /// <summary>
        /// Lấy transactions pending (chưa complete)
        /// Dùng cho monitoring, timeout handling
        /// </summary>
        public async Task<IEnumerable<Transaction>> GetPendingTransactionsAsync()
        {
            return await dbSet
                // ❌ REMOVED: .Include(t => t.Order)
                .Where(t => t.Status == TransactionStatus.Pending)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy completed transactions
        /// </summary>
        public async Task<IEnumerable<Transaction>> GetCompletedTransactionsAsync(
            DateTime startDate,
            DateTime endDate)
        {
            return await dbSet
                .Where(t => t.Status == TransactionStatus.Completed
                         && t.CompletedAt.HasValue
                         && t.CompletedAt >= startDate
                         && t.CompletedAt <= endDate)
                .OrderByDescending(t => t.CompletedAt)
                .ToListAsync();
        }
    }
}