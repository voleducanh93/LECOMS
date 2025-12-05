using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IShopWalletService
    {
        Task<ShopWallet> GetOrCreateWalletAsync(int shopId);

        Task<ShopWallet?> GetWalletWithTransactionsAsync(
            int shopId, int pageNumber = 1, int pageSize = 20);

        Task<ShopWallet> AddPendingBalanceAsync(
            int shopId, decimal amount, string orderId, string description);

        Task<ShopWallet> ReleaseBalanceAsync(int shopId, decimal amount, string orderId);

        Task<ShopWallet> DeductBalanceAsync(
            int shopId,
            decimal amount,
            WalletTransactionType type,
            string referenceId,
            string description);

        Task<ShopWallet> AddAvailableBalanceAsync(
            int shopId, decimal amount, string referenceId, string description);

        Task<bool> CanWithdrawAsync(int shopId, decimal amount);

        Task<WalletSummaryDto> GetWalletSummaryAsync(int shopId);
        Task<ShopWallet> DeductPendingOnlyAsync(int shopId, decimal amount, WalletTransactionType type, string referenceId, string description);
    }


    public class WalletSummaryDto
    {
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal TotalEarned { get; set; }
        public decimal TotalWithdrawn { get; set; }
        public decimal TotalRefunded { get; set; }
        public int PendingOrdersCount { get; set; }
        // SellerDashboard-specific
        public decimal PendingWithdrawalAmount { get; set; }
        public decimal ApprovedWithdrawalAmount { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
