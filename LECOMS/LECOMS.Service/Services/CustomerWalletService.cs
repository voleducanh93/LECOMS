using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class CustomerWalletService : ICustomerWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CustomerWalletService> _logger;

        public CustomerWalletService(IUnitOfWork unitOfWork, ILogger<CustomerWalletService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CustomerWallet> GetOrCreateWalletAsync(string customerId)
        {
            var wallet = await _unitOfWork.CustomerWallets.GetByCustomerIdAsync(customerId);
            if (wallet == null)
            {
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

                // Nếu helper tự đứng 1 mình — lưu ngay.
                if (!_unitOfWork.HasActiveTransaction)
                {
                    await _unitOfWork.CompleteAsync();
                }
            }
            return wallet;
        }

        public async Task<CustomerWallet?> GetWalletWithTransactionsAsync(
            string customerId, int pageNumber = 1, int pageSize = 20)
        {
            return await _unitOfWork.CustomerWallets
                .GetByCustomerIdAsync(customerId, includeTransactions: true);
        }

        public async Task<CustomerWallet> AddBalanceAsync(
            string customerId, decimal amount, string refundId, string description)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));

            var wallet = await GetOrCreateWalletAsync(customerId);
            decimal before = wallet.Balance;

            wallet.Balance += amount;
            wallet.TotalRefunded += amount;
            wallet.LastUpdated = DateTime.UtcNow;

            //await _unitOfWork.CustomerWallets.UpdateAsync(wallet);

            var walletTx = new CustomerWalletTransaction
            {
                Id = Guid.NewGuid().ToString(),
                CustomerWalletId = wallet.Id,
                Type = WalletTransactionType.Refund,
                Amount = amount,
                BalanceBefore = before,
                BalanceAfter = wallet.Balance,
                Description = description,
                ReferenceId = refundId,
                ReferenceType = "RefundRequest",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.CustomerWalletTransactions.AddAsync(walletTx);

            // Nếu hiện tại KHÔNG có transaction bao ngoài thì tự save luôn.
            if (!_unitOfWork.HasActiveTransaction)
            {
                await _unitOfWork.CompleteAsync();
            }

            return wallet;
        }

        public async Task<CustomerWallet> DeductBalanceAsync(
            string customerId,
            decimal amount,
            WalletTransactionType type,
            string referenceId,
            string description)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));

            var wallet = await _unitOfWork.CustomerWallets.GetByCustomerIdAsync(customerId);
            if (wallet == null)
                throw new InvalidOperationException($"Wallet not found for Customer {customerId}");
            if (wallet.Balance < amount)
                throw new InvalidOperationException(
                    $"Insufficient balance. Available: {wallet.Balance}, Required: {amount}");

            var before = wallet.Balance;
            wallet.Balance -= amount;
            if (type == WalletTransactionType.Withdrawal) wallet.TotalWithdrawn += amount;
            else if (type == WalletTransactionType.Payment) wallet.TotalSpent += amount;
            wallet.LastUpdated = DateTime.UtcNow;

           // await _unitOfWork.CustomerWallets.UpdateAsync(wallet);

            var walletTx = new CustomerWalletTransaction
            {
                Id = Guid.NewGuid().ToString(),
                CustomerWalletId = wallet.Id,
                Type = type,
                Amount = -amount,
                BalanceBefore = before,
                BalanceAfter = wallet.Balance,
                Description = description,
                ReferenceId = referenceId,
                ReferenceType = type.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.CustomerWalletTransactions.AddAsync(walletTx);

            // Nếu không có transaction ngoài (ví dụ gọi lẻ), tự SaveChanges.
            if (!_unitOfWork.HasActiveTransaction)
            {
                await _unitOfWork.CompleteAsync();
            }

            return wallet;
        }

        public async Task<bool> HasSufficientBalanceAsync(string customerId, decimal amount)
        {
            var wallet = await _unitOfWork.CustomerWallets.GetByCustomerIdAsync(customerId);
            return wallet != null && wallet.Balance >= amount;
        }

        public async Task<decimal> GetBalanceAsync(string customerId)
        {
            var wallet = await GetOrCreateWalletAsync(customerId);
            return wallet.Balance;
        }
    }
}
