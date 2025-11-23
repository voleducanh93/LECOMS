using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class ShopWalletService : IShopWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ShopWalletService> _logger;

        public ShopWalletService(IUnitOfWork unitOfWork, ILogger<ShopWalletService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ShopWallet> GetOrCreateWalletAsync(int shopId)
        {
            var wallet = await _unitOfWork.ShopWallets.GetByShopIdAsync(shopId);

            if (wallet == null)
            {
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

        public async Task<ShopWallet?> GetWalletWithTransactionsAsync(
            int shopId, int pageNumber = 1, int pageSize = 20)
        {
            return await _unitOfWork.ShopWallets
                .GetByShopIdAsync(shopId, includeTransactions: true);
        }

        public async Task<ShopWallet> AddPendingBalanceAsync(
            int shopId, decimal amount, string orderId, string description)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));

            var wallet = await GetOrCreateWalletAsync(shopId);

            decimal before = wallet.PendingBalance;

            wallet.PendingBalance += amount;
            wallet.TotalEarned += amount;
            wallet.LastUpdated = DateTime.UtcNow;

            await _unitOfWork.ShopWallets.UpdateAsync(wallet);

            await _unitOfWork.WalletTransactions.AddAsync(new WalletTransaction
            {
                Id = Guid.NewGuid().ToString(),
                ShopWalletId = wallet.Id,
                Type = WalletTransactionType.OrderRevenue,
                Amount = amount,
                BalanceBefore = before,
                BalanceAfter = wallet.PendingBalance,
                BalanceType = "Pending",
                Description = description,
                ReferenceId = orderId,
                ReferenceType = "Order",
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.CompleteAsync();
            return wallet;
        }

        public async Task<ShopWallet> ReleaseBalanceAsync(int shopId, decimal amount, string orderId)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));

            var wallet = await GetOrCreateWalletAsync(shopId);

            if (wallet.PendingBalance < amount)
                throw new InvalidOperationException("Insufficient pending balance.");

            decimal beforePending = wallet.PendingBalance;
            decimal beforeAvailable = wallet.AvailableBalance;

            wallet.PendingBalance -= amount;
            wallet.AvailableBalance += amount;
            wallet.LastUpdated = DateTime.UtcNow;

            await _unitOfWork.ShopWallets.UpdateAsync(wallet);

            await _unitOfWork.WalletTransactions.AddAsync(new WalletTransaction
            {
                Id = Guid.NewGuid().ToString(),
                ShopWalletId = wallet.Id,
                Type = WalletTransactionType.BalanceRelease,
                Amount = -amount,
                BalanceBefore = beforePending,
                BalanceAfter = wallet.PendingBalance,
                BalanceType = "Pending",
                Description = $"Release pending revenue for order {orderId}",
                ReferenceId = orderId,
                ReferenceType = "Order",
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.WalletTransactions.AddAsync(new WalletTransaction
            {
                Id = Guid.NewGuid().ToString(),
                ShopWalletId = wallet.Id,
                Type = WalletTransactionType.BalanceRelease,
                Amount = amount,
                BalanceBefore = beforeAvailable,
                BalanceAfter = wallet.AvailableBalance,
                BalanceType = "Available",
                Description = $"Revenue available for order {orderId}",
                ReferenceId = orderId,
                ReferenceType = "Order",
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.CompleteAsync();
            return wallet;
        }

        public async Task<ShopWallet> DeductBalanceAsync(
            int shopId,
            decimal amount,
            WalletTransactionType type,
            string referenceId,
            string description)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));

            var wallet = await GetOrCreateWalletAsync(shopId);

            decimal availableBefore = wallet.AvailableBalance;
            decimal pendingBefore = wallet.PendingBalance;

            decimal deductAvailable = Math.Min(amount, wallet.AvailableBalance);
            decimal deductPending = amount - deductAvailable;

            if (deductPending > wallet.PendingBalance)
                throw new InvalidOperationException("Insufficient balance.");

            wallet.AvailableBalance -= deductAvailable;
            wallet.PendingBalance -= deductPending;
            wallet.LastUpdated = DateTime.UtcNow;

            await _unitOfWork.ShopWallets.UpdateAsync(wallet);

            if (deductAvailable > 0)
            {
                await _unitOfWork.WalletTransactions.AddAsync(new WalletTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    ShopWalletId = wallet.Id,
                    Type = type,
                    Amount = -deductAvailable,
                    BalanceBefore = availableBefore,
                    BalanceAfter = wallet.AvailableBalance,
                    BalanceType = "Available",
                    Description = description,
                    ReferenceId = referenceId,
                    ReferenceType = type.ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (deductPending > 0)
            {
                await _unitOfWork.WalletTransactions.AddAsync(new WalletTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    ShopWalletId = wallet.Id,
                    Type = type,
                    Amount = -deductPending,
                    BalanceBefore = pendingBefore,
                    BalanceAfter = wallet.PendingBalance,
                    BalanceType = "Pending",
                    Description = description,
                    ReferenceId = referenceId,
                    ReferenceType = type.ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (type == WalletTransactionType.Refund)
                wallet.TotalRefunded += amount;
            else if (type == WalletTransactionType.Withdrawal)
                wallet.TotalWithdrawn += amount;

            await _unitOfWork.CompleteAsync();
            return wallet;
        }

        public async Task<ShopWallet> AddAvailableBalanceAsync(
            int shopId, decimal amount, string referenceId, string description)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive", nameof(amount));

            var wallet = await GetOrCreateWalletAsync(shopId);

            decimal before = wallet.AvailableBalance;

            wallet.AvailableBalance += amount;
            wallet.LastUpdated = DateTime.UtcNow;

            await _unitOfWork.ShopWallets.UpdateAsync(wallet);

            await _unitOfWork.WalletTransactions.AddAsync(new WalletTransaction
            {
                Id = Guid.NewGuid().ToString(),
                ShopWalletId = wallet.Id,
                Type = WalletTransactionType.Refund,
                Amount = amount,
                BalanceBefore = before,
                BalanceAfter = wallet.AvailableBalance,
                BalanceType = "Available",
                Description = description,
                ReferenceId = referenceId,
                ReferenceType = "RefundRequest",
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.CompleteAsync();
            return wallet;
        }

        public async Task<bool> CanWithdrawAsync(int shopId, decimal amount)
        {
            var wallet = await GetOrCreateWalletAsync(shopId);
            return wallet.AvailableBalance >= amount;
        }

        public async Task<WalletSummaryDto> GetWalletSummaryAsync(int shopId)
        {
            var wallet = await GetOrCreateWalletAsync(shopId);

            var pendingOrdersCount = await _unitOfWork.Orders.CountAsync(
                o => o.ShopId == shopId &&
                     o.PaymentStatus == PaymentStatus.Paid &&
                     !o.BalanceReleased);

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

        public async Task<ShopWallet> DeductPendingOnlyAsync(int shopId, decimal amount, WalletTransactionType type, string referenceId,string description)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive", nameof(amount));

            var wallet = await GetOrCreateWalletAsync(shopId);

            if (wallet.PendingBalance < amount)
                throw new InvalidOperationException("Insufficient pending balance.");

            decimal before = wallet.PendingBalance;

            wallet.PendingBalance -= amount;
            wallet.TotalRefunded += amount;
            wallet.LastUpdated = DateTime.UtcNow;

            await _unitOfWork.ShopWallets.UpdateAsync(wallet);

            await _unitOfWork.WalletTransactions.AddAsync(new WalletTransaction
            {
                Id = Guid.NewGuid().ToString(),
                ShopWalletId = wallet.Id,
                Type = type,
                Amount = -amount,
                BalanceBefore = before,
                BalanceAfter = wallet.PendingBalance,
                BalanceType = "Pending",
                Description = description,
                ReferenceId = referenceId,
                ReferenceType = "Refund",
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.CompleteAsync();
            return wallet;
        }

    }
}
