using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    /// <summary>
    /// Service implementation cho ShopWallet
    /// ⚠️ IMPORTANT: Không tạo transaction riêng trong các methods
    /// Transaction được quản lý bởi caller (PaymentService, OrderService)
    /// </summary>
    public class ShopWalletService : IShopWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ShopWalletService> _logger;

        public ShopWalletService(IUnitOfWork unitOfWork, ILogger<ShopWalletService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Lấy hoặc tạo mới wallet cho shop
        /// </summary>
        public async Task<ShopWallet> GetOrCreateWalletAsync(int shopId)
        {
            var wallet = await _unitOfWork.ShopWallets.GetByShopIdAsync(shopId);

            if (wallet == null)
            {
                _logger.LogInformation("Creating new wallet for Shop: {ShopId}", shopId);

                wallet = new ShopWallet
                {
                    Id = Guid.NewGuid().ToString(),
                    ShopId = shopId,
                    AvailableBalance = 0,
                    PendingBalance = 0,
                    TotalEarned = 0,
                    TotalWithdrawn = 0,
                    TotalRefunded = 0,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

                await _unitOfWork.ShopWallets.AddAsync(wallet);
                await _unitOfWork.CompleteAsync();
            }

            return wallet;
        }

        /// <summary>
        /// Lấy wallet với transactions
        /// </summary>
        public async Task<ShopWallet?> GetWalletWithTransactionsAsync(int shopId, int pageNumber = 1, int pageSize = 20)
        {
            return await _unitOfWork.ShopWallets.GetByShopIdAsync(shopId, includeTransactions: true);
        }

        /// <summary>
        /// Cộng tiền vào PendingBalance
        /// ⚠️ Không tạo transaction riêng
        /// </summary>
        public async Task<ShopWallet> AddPendingBalanceAsync(int shopId, decimal amount, string orderId, string description)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive", nameof(amount));

            _logger.LogInformation("Adding {Amount:N0} to PendingBalance for Shop {ShopId}", amount, shopId);

            try
            {
                // 1. Lấy hoặc tạo wallet
                var wallet = await GetOrCreateWalletAsync(shopId);

                decimal balanceBefore = wallet.PendingBalance;

                // 2. Cộng tiền vào PendingBalance
                wallet.PendingBalance += amount;
                wallet.TotalEarned += amount;
                wallet.LastUpdated = DateTime.UtcNow;

                await _unitOfWork.ShopWallets.UpdateAsync(wallet);

                // 3. Ghi log WalletTransaction
                var walletTransaction = new WalletTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    ShopWalletId = wallet.Id,
                    Type = WalletTransactionType.OrderRevenue,
                    Amount = amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.PendingBalance,
                    BalanceType = "Pending",
                    Description = description,
                    ReferenceId = orderId,
                    ReferenceType = "Order",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.WalletTransactions.AddAsync(walletTransaction);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("✅ Shop {ShopId}: PendingBalance {Before:N0} → {After:N0} (+{Amount:N0})",
                    shopId, balanceBefore, wallet.PendingBalance, amount);

                return wallet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error adding pending balance for Shop {ShopId}", shopId);
                throw;
            }
        }

        /// <summary>
        /// Release balance: Chuyển từ Pending sang Available
        /// ⚠️ Không tạo transaction riêng
        /// </summary>
        public async Task<ShopWallet> ReleaseBalanceAsync(int shopId, decimal amount, string orderId)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive", nameof(amount));

            _logger.LogInformation("Releasing {Amount:N0} for Shop {ShopId}", amount, shopId);

            try
            {
                var wallet = await _unitOfWork.ShopWallets.GetByShopIdAsync(shopId);
                if (wallet == null)
                    throw new InvalidOperationException($"Wallet not found for Shop {shopId}");

                if (wallet.PendingBalance < amount)
                    throw new InvalidOperationException(
                        $"Insufficient pending balance. Available: {wallet.PendingBalance:N0}, Required: {amount:N0}");

                decimal pendingBefore = wallet.PendingBalance;
                decimal availableBefore = wallet.AvailableBalance;

                // 1. Trừ từ Pending
                wallet.PendingBalance -= amount;

                // 2. Cộng vào Available
                wallet.AvailableBalance += amount;
                wallet.LastUpdated = DateTime.UtcNow;

                await _unitOfWork.ShopWallets.UpdateAsync(wallet);

                // 3. Ghi log 2 transactions
                // 3a. Trừ Pending
                var deductPendingTx = new WalletTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    ShopWalletId = wallet.Id,
                    Type = WalletTransactionType.BalanceRelease,
                    Amount = -amount,
                    BalanceBefore = pendingBefore,
                    BalanceAfter = wallet.PendingBalance,
                    BalanceType = "Pending",
                    Description = $"Release balance cho đơn hàng {orderId}",
                    ReferenceId = orderId,
                    ReferenceType = "Order",
                    CreatedAt = DateTime.UtcNow
                };

                // 3b. Cộng Available
                var addAvailableTx = new WalletTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    ShopWalletId = wallet.Id,
                    Type = WalletTransactionType.BalanceRelease,
                    Amount = amount,
                    BalanceBefore = availableBefore,
                    BalanceAfter = wallet.AvailableBalance,
                    BalanceType = "Available",
                    Description = $"Nhận tiền từ đơn hàng {orderId}",
                    ReferenceId = orderId,
                    ReferenceType = "Order",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.WalletTransactions.AddAsync(deductPendingTx);
                await _unitOfWork.WalletTransactions.AddAsync(addAvailableTx);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("✅ Shop {ShopId}: Balance released. Pending: {Pending:N0}, Available: {Available:N0}",
                    shopId, wallet.PendingBalance, wallet.AvailableBalance);

                return wallet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error releasing balance for Shop {ShopId}", shopId);
                throw;
            }
        }

        /// <summary>
        /// Trừ tiền từ wallet
        /// ⚠️ Không tạo transaction riêng
        /// </summary>
        public async Task<ShopWallet> DeductBalanceAsync(
            int shopId,
            decimal amount,
            WalletTransactionType type,
            string referenceId,
            string description)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive", nameof(amount));

            _logger.LogInformation("Deducting {Amount:N0} from Shop {ShopId} for {Type}", amount, shopId, type);

            try
            {
                var wallet = await _unitOfWork.ShopWallets.GetByShopIdAsync(shopId);
                if (wallet == null)
                    throw new InvalidOperationException($"Wallet not found for Shop {shopId}");

                decimal totalBalance = wallet.AvailableBalance + wallet.PendingBalance;
                if (totalBalance < amount)
                    throw new InvalidOperationException(
                        $"Insufficient balance. Total: {totalBalance:N0}, Required: {amount:N0}");

                // Ưu tiên trừ từ Available, sau đó Pending
                decimal deductFromAvailable = Math.Min(amount, wallet.AvailableBalance);
                decimal deductFromPending = amount - deductFromAvailable;

                if (deductFromAvailable > 0)
                {
                    decimal availableBefore = wallet.AvailableBalance;
                    wallet.AvailableBalance -= deductFromAvailable;

                    var tx = new WalletTransaction
                    {
                        Id = Guid.NewGuid().ToString(),
                        ShopWalletId = wallet.Id,
                        Type = type,
                        Amount = -deductFromAvailable,
                        BalanceBefore = availableBefore,
                        BalanceAfter = wallet.AvailableBalance,
                        BalanceType = "Available",
                        Description = description,
                        ReferenceId = referenceId,
                        ReferenceType = type.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.WalletTransactions.AddAsync(tx);
                }

                if (deductFromPending > 0)
                {
                    decimal pendingBefore = wallet.PendingBalance;
                    wallet.PendingBalance -= deductFromPending;

                    var tx = new WalletTransaction
                    {
                        Id = Guid.NewGuid().ToString(),
                        ShopWalletId = wallet.Id,
                        Type = type,
                        Amount = -deductFromPending,
                        BalanceBefore = pendingBefore,
                        BalanceAfter = wallet.PendingBalance,
                        BalanceType = "Pending",
                        Description = description,
                        ReferenceId = referenceId,
                        ReferenceType = type.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.WalletTransactions.AddAsync(tx);
                }

                // Update statistics
                if (type == WalletTransactionType.Refund)
                {
                    wallet.TotalRefunded += amount;
                }
                else if (type == WalletTransactionType.Withdrawal)
                {
                    wallet.TotalWithdrawn += amount;
                }

                wallet.LastUpdated = DateTime.UtcNow;
                await _unitOfWork.ShopWallets.UpdateAsync(wallet);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("✅ Deducted {Amount:N0} from Shop {ShopId}", amount, shopId);

                return wallet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deducting balance for Shop {ShopId}", shopId);
                throw;
            }
        }

        /// <summary>
        /// Cộng tiền vào AvailableBalance
        /// ⚠️ Không tạo transaction riêng
        /// </summary>
        public async Task<ShopWallet> AddAvailableBalanceAsync(int shopId, decimal amount, string referenceId, string description)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive", nameof(amount));

            _logger.LogInformation("Adding {Amount:N0} to AvailableBalance for Shop {ShopId}", amount, shopId);

            try
            {
                var wallet = await GetOrCreateWalletAsync(shopId);

                decimal balanceBefore = wallet.AvailableBalance;

                wallet.AvailableBalance += amount;
                wallet.LastUpdated = DateTime.UtcNow;

                await _unitOfWork.ShopWallets.UpdateAsync(wallet);

                var walletTransaction = new WalletTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    ShopWalletId = wallet.Id,
                    Type = WalletTransactionType.Refund,
                    Amount = amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.AvailableBalance,
                    BalanceType = "Available",
                    Description = description,
                    ReferenceId = referenceId,
                    ReferenceType = "RefundRequest",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.WalletTransactions.AddAsync(walletTransaction);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("✅ Added {Amount:N0} to AvailableBalance for Shop {ShopId}", amount, shopId);

                return wallet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error adding available balance for Shop {ShopId}", shopId);
                throw;
            }
        }

        /// <summary>
        /// Kiểm tra shop có đủ balance để rút tiền không
        /// </summary>
        public async Task<bool> CanWithdrawAsync(int shopId, decimal amount)
        {
            var wallet = await _unitOfWork.ShopWallets.GetByShopIdAsync(shopId);
            return wallet != null && wallet.AvailableBalance >= amount;
        }

        /// <summary>
        /// Lấy wallet summary
        /// </summary>
        public async Task<WalletSummaryDto> GetWalletSummaryAsync(int shopId)
        {
            var wallet = await GetOrCreateWalletAsync(shopId);

            // Count pending orders
            var pendingOrdersCount = await _unitOfWork.Orders.CountAsync(
                o => o.ShopId == shopId
                    && o.PaymentStatus == PaymentStatus.Paid
                    && !o.BalanceReleased);

            return new WalletSummaryDto
            {
                AvailableBalance = wallet.AvailableBalance,
                PendingBalance = wallet.PendingBalance,
                TotalEarned = wallet.TotalEarned,
                TotalWithdrawn = wallet.TotalWithdrawn,
                TotalRefunded = wallet.TotalRefunded,
                PendingOrdersCount = pendingOrdersCount,
                LastUpdated = wallet.LastUpdated
            };
        }
    }
}