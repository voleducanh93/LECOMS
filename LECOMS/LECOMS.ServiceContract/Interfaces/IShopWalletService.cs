using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    /// <summary>
    /// Service quản lý ShopWallet
    /// ⚠️ NOTE: Methods không tạo transaction riêng
    /// Transaction được quản lý bởi caller (PaymentService, OrderService)
    /// </summary>
    public interface IShopWalletService
    {
        /// <summary>
        /// Lấy hoặc tạo mới wallet cho shop
        /// Auto-create nếu chưa có
        /// </summary>
        Task<ShopWallet> GetOrCreateWalletAsync(int shopId);

        /// <summary>
        /// Lấy wallet với transactions
        /// </summary>
        Task<ShopWallet?> GetWalletWithTransactionsAsync(int shopId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Cộng tiền vào PendingBalance
        /// Dùng khi order payment success
        /// 
        /// ⚠️ Không tạo transaction riêng, sử dụng transaction từ caller
        /// 
        /// FLOW:
        /// 1. Lấy wallet (hoặc tạo mới)
        /// 2. Cộng amount vào PendingBalance
        /// 3. Cộng amount vào TotalEarned
        /// 4. Update LastUpdated
        /// 5. Ghi log WalletTransaction
        /// </summary>
        Task<ShopWallet> AddPendingBalanceAsync(int shopId, decimal amount, string orderId, string description);

        /// <summary>
        /// Release balance: Chuyển từ Pending sang Available
        /// Dùng khi order đã qua holding period
        /// 
        /// ⚠️ Không tạo transaction riêng
        /// 
        /// FLOW:
        /// 1. Kiểm tra PendingBalance >= amount
        /// 2. Trừ PendingBalance
        /// 3. Cộng AvailableBalance
        /// 4. Ghi log WalletTransaction (Type = BalanceRelease)
        /// </summary>
        Task<ShopWallet> ReleaseBalanceAsync(int shopId, decimal amount, string orderId);

        /// <summary>
        /// Trừ tiền từ wallet
        /// Dùng khi:
        /// - Withdrawal approved (trừ từ AvailableBalance)
        /// - Refund to customer (trừ từ Available hoặc Pending)
        /// 
        /// ⚠️ Không tạo transaction riêng
        /// 
        /// FLOW:
        /// 1. Kiểm tra balance đủ không
        /// 2. Trừ tiền (ưu tiên Available, sau đó Pending)
        /// 3. Ghi log WalletTransaction
        /// </summary>
        Task<ShopWallet> DeductBalanceAsync(
            int shopId,
            decimal amount,
            WalletTransactionType type,
            string referenceId,
            string description);

        /// <summary>
        /// Cộng tiền vào AvailableBalance
        /// Dùng khi: Refund from customer
        /// 
        /// ⚠️ Không tạo transaction riêng
        /// </summary>
        Task<ShopWallet> AddAvailableBalanceAsync(int shopId, decimal amount, string referenceId, string description);

        /// <summary>
        /// Kiểm tra shop có đủ balance để rút tiền không
        /// </summary>
        Task<bool> CanWithdrawAsync(int shopId, decimal amount);

        /// <summary>
        /// Lấy wallet summary (cho dashboard)
        /// </summary>
        Task<WalletSummaryDto> GetWalletSummaryAsync(int shopId);
    }

    /// <summary>
    /// DTO cho wallet summary
    /// </summary>
    public class WalletSummaryDto
    {
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal TotalEarned { get; set; }
        public decimal TotalWithdrawn { get; set; }
        public decimal TotalRefunded { get; set; }
        public int PendingOrdersCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}