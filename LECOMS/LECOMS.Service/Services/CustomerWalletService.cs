using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    /// <summary>
    /// Service implementation cho CustomerWallet
    /// Author: haupdse170479
    /// Created: 2025-01-06
    /// </summary>
    public class CustomerWalletService : ICustomerWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CustomerWalletService> _logger;

        public CustomerWalletService(IUnitOfWork unitOfWork, ILogger<CustomerWalletService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Lấy hoặc tạo mới wallet cho customer
        /// </summary>
        public async Task<CustomerWallet> GetOrCreateWalletAsync(string customerId)
        {
            var wallet = await _unitOfWork.CustomerWallets.GetByCustomerIdAsync(customerId);

            if (wallet == null)
            {
                _logger.LogInformation("Creating new wallet for Customer: {CustomerId}", customerId);

                wallet = new CustomerWallet
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = customerId,
                    Balance = 0,
                    TotalRefunded = 0,
                    TotalSpent = 0,
                    TotalWithdrawn = 0,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

                await _unitOfWork.CustomerWallets.AddAsync(wallet);
                await _unitOfWork.CompleteAsync();
            }

            return wallet;
        }

        /// <summary>
        /// Lấy wallet với transactions
        /// </summary>
        public async Task<CustomerWallet?> GetWalletWithTransactionsAsync(string customerId, int pageNumber = 1, int pageSize = 20)
        {
            return await _unitOfWork.CustomerWallets.GetByCustomerIdAsync(customerId, includeTransactions: true);
        }

        /// <summary>
        /// Cộng tiền vào wallet (từ refund)
        /// </summary>
        public async Task<CustomerWallet> AddBalanceAsync(string customerId, decimal amount, string refundId, string description)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive", nameof(amount));

            _logger.LogInformation("Adding {Amount} to CustomerWallet for {CustomerId}", amount, customerId);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Lấy hoặc tạo wallet
                var wallet = await GetOrCreateWalletAsync(customerId);

                decimal balanceBefore = wallet.Balance;

                // 2. Cộng tiền vào Balance
                wallet.Balance += amount;
                wallet.TotalRefunded += amount;
                wallet.LastUpdated = DateTime.UtcNow;

                await _unitOfWork.CustomerWallets.UpdateAsync(wallet);

                // 3. Ghi log CustomerWalletTransaction
                var walletTransaction = new CustomerWalletTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerWalletId = wallet.Id,
                    Type = WalletTransactionType.Refund,
                    Amount = amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Description = description,
                    ReferenceId = refundId,
                    ReferenceType = "RefundRequest",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.CustomerWalletTransactions.AddAsync(walletTransaction);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Added {Amount} to CustomerWallet. Customer {CustomerId} balance: {Balance}",
                    amount, customerId, wallet.Balance);

                return wallet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding balance for Customer {CustomerId}", customerId);
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Trừ tiền từ wallet
        /// </summary>
        public async Task<CustomerWallet> DeductBalanceAsync(
            string customerId,
            decimal amount,
            WalletTransactionType type,
            string referenceId,
            string description)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive", nameof(amount));

            _logger.LogInformation("Deducting {Amount} from CustomerWallet for {CustomerId}", amount, customerId);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _unitOfWork.CustomerWallets.GetByCustomerIdAsync(customerId);
                if (wallet == null)
                    throw new InvalidOperationException($"Wallet not found for Customer {customerId}");

                if (wallet.Balance < amount)
                    throw new InvalidOperationException($"Insufficient balance. Available: {wallet.Balance}, Required: {amount}");

                decimal balanceBefore = wallet.Balance;

                // Trừ tiền
                wallet.Balance -= amount;

                // Update statistics
                if (type == WalletTransactionType.Withdrawal)
                {
                    wallet.TotalWithdrawn += amount;
                }
                else if (type == WalletTransactionType.Payment)
                {
                    wallet.TotalSpent += amount;
                }

                wallet.LastUpdated = DateTime.UtcNow;

                await _unitOfWork.CustomerWallets.UpdateAsync(wallet);

                // Ghi log
                var walletTransaction = new CustomerWalletTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerWalletId = wallet.Id,
                    Type = type,
                    Amount = -amount, // Âm = trừ
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Description = description,
                    ReferenceId = referenceId,
                    ReferenceType = type.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.CustomerWalletTransactions.AddAsync(walletTransaction);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deducted {Amount} from CustomerWallet. Balance: {Balance}", amount, wallet.Balance);

                return wallet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deducting balance for Customer {CustomerId}", customerId);
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Kiểm tra customer có đủ balance không
        /// </summary>
        public async Task<bool> HasSufficientBalanceAsync(string customerId, decimal amount)
        {
            var wallet = await _unitOfWork.CustomerWallets.GetByCustomerIdAsync(customerId);
            return wallet != null && wallet.Balance >= amount;
        }

        /// <summary>
        /// Lấy balance hiện tại
        /// </summary>
        public async Task<decimal> GetBalanceAsync(string customerId)
        {
            var wallet = await GetOrCreateWalletAsync(customerId);
            return wallet.Balance;
        }
    }
}