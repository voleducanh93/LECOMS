using LECOMS.Data.DTOs.Wallet;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class PlatformWalletService : IPlatformWalletService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PlatformWalletService> _logger;

        // Id singleton cho ví sàn (có thể hard-code hoặc lấy từ config)
        private const string PLATFORM_WALLET_SINGLETON_ID = "PLATFORM_WALLET_SINGLETON";

        public PlatformWalletService(IUnitOfWork uow, ILogger<PlatformWalletService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<PlatformWallet> GetOrCreateAsync()
        {
            var wallet = await _uow.PlatformWallets.GetAsync(w => w.Id == PLATFORM_WALLET_SINGLETON_ID);

            if (wallet == null)
            {
                wallet = new PlatformWallet
                {
                    Id = PLATFORM_WALLET_SINGLETON_ID,
                    Balance = 0,
                    TotalCommissionEarned = 0,
                    TotalCommissionRefunded = 0,
                    TotalPayout = 0,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

                await _uow.PlatformWallets.AddAsync(wallet);
                await _uow.CompleteAsync();
            }

            return wallet;
        }

        private async Task<PlatformWallet> AddTransactionAsync(
            decimal amount,
            PlatformWalletTransactionType type,
            string referenceId,
            string referenceType,
            string description)
        {
            var wallet = await GetOrCreateAsync();

            decimal before = wallet.Balance;
            wallet.Balance += amount;
            wallet.LastUpdated = DateTime.UtcNow;

            if (type == PlatformWalletTransactionType.CommissionIncome && amount > 0)
                wallet.TotalCommissionEarned += amount;
            if (type == PlatformWalletTransactionType.CommissionRefund && amount < 0)
                wallet.TotalCommissionRefunded += -amount; // lưu số dương
            if (type == PlatformWalletTransactionType.PayoutToBank && amount < 0)
                wallet.TotalPayout += -amount;

            await _uow.PlatformWallets.UpdateAsync(wallet);

            var tx = new PlatformWalletTransaction
            {
                PlatformWalletId = wallet.Id,
                Amount = amount,
                Type = type,
                BalanceBefore = before,
                BalanceAfter = wallet.Balance,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.PlatformWalletTransactions.AddAsync(tx);
            await _uow.CompleteAsync();

            return wallet;
        }

        public Task<PlatformWallet> AddCommissionAsync(
            decimal commissionAmount,
            string transactionId,
            string orderCodesDescription)
        {
            if (commissionAmount <= 0)
                throw new ArgumentException("Commission Số tiền phải dương");

            string desc = $"Commission from transaction {transactionId} ({orderCodesDescription})";

            return AddTransactionAsync(
                commissionAmount,
                PlatformWalletTransactionType.CommissionIncome,
                transactionId,
                "Transaction",
                desc);
        }

        public Task<PlatformWallet> RefundCommissionAsync(
            decimal commissionRefundAmount,
            string refundId,
            string description)
        {
            if (commissionRefundAmount <= 0)
                throw new ArgumentException("Commission refund Số tiền phải dương");

            // Hoàn hoa hồng là tiền ra khỏi ví → amount âm
            decimal amount = -commissionRefundAmount;

            return AddTransactionAsync(
                amount,
                PlatformWalletTransactionType.CommissionRefund,
                refundId,
                "Refund",
                description);
        }

        public Task<PlatformWallet> PayoutAsync(
            decimal amount,
            string payoutId,
            string description)
        {
            if (amount <= 0)
                throw new ArgumentException("Thanh toán Số tiền phải dương");

            // Payout = tiền ra khỏi ví → -amount
            return AddTransactionAsync(
                -amount,
                PlatformWalletTransactionType.PayoutToBank,
                payoutId,
                "Payout",
                description);
        }

        public Task<PlatformWallet> ManualAdjustAsync(decimal amount, string description)
        {
            // amount > 0: cộng thêm, < 0: trừ bớt
            return AddTransactionAsync(
                amount,
                PlatformWalletTransactionType.ManualAdjust,
                referenceId: Guid.NewGuid().ToString(),
                referenceType: "Manual",
                description: description ?? "Manual adjust");
        }

        public async Task<PlatformWalletDTO> GetSummaryAsync()
        {
            var wallet = await GetOrCreateAsync();

            return new PlatformWalletDTO
            {
                Balance = wallet.Balance,
                TotalCommissionEarned = wallet.TotalCommissionEarned,
                TotalCommissionRefunded = wallet.TotalCommissionRefunded,
                TotalPayout = wallet.TotalPayout,
                LastUpdated = wallet.LastUpdated
            };
        }

        public async Task<IEnumerable<PlatformWalletTransactionDTO>> GetTransactionsAsync(
            int pageNumber = 1,
            int pageSize = 50)
        {
            var list = await _uow.PlatformWalletTransactions.GetAllAsync(
                filter: x => x.PlatformWalletId == PLATFORM_WALLET_SINGLETON_ID);

            var paged = list
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return paged.Select(x => new PlatformWalletTransactionDTO
            {
                Id = x.Id,
                Amount = x.Amount,
                Type = x.Type.ToString(),
                BalanceBefore = x.BalanceBefore,
                BalanceAfter = x.BalanceAfter,
                ReferenceId = x.ReferenceId,
                ReferenceType = x.ReferenceType,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            });
        }
    }
}
