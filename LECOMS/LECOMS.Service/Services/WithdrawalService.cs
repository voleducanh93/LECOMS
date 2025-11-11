using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    /// <summary>
    /// Service xử lý rút tiền từ ShopWallet
    /// 
    /// FLOW:
    /// 1. Shop tạo request → Pending
    /// 2. Admin approve → TRỪ TIỀN NGAY (lock), Status = Approved
    /// 3. Background job xử lý chuyển khoản → Completed/Failed
    /// 4. Nếu Failed → HOÀN TIỀN vào wallet
    /// </summary>
    public class WithdrawalService : IWithdrawalService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IShopWalletService _shopWalletService;
        private readonly ILogger<WithdrawalService> _logger;

        public WithdrawalService(
            IUnitOfWork unitOfWork,
            IShopWalletService shopWalletService,
            ILogger<WithdrawalService> logger)
        {
            _unitOfWork = unitOfWork;
            _shopWalletService = shopWalletService;
            _logger = logger;
        }

        /// <summary>
        /// Shop tạo withdrawal request
        /// </summary>
        public async Task<WithdrawalRequest> CreateWithdrawalRequestAsync(CreateWithdrawalRequestDto dto)
        {
            _logger.LogInformation("Creating withdrawal request for Shop {ShopId}, Amount: {Amount}", dto.ShopId, dto.Amount);

            // 1. Validate shop
            var shop = await _unitOfWork.Shops.GetAsync(s => s.Id == dto.ShopId);
            if (shop == null)
                throw new InvalidOperationException($"Shop {dto.ShopId} not found");

            // 2. Lấy platform config
            var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

            // 3. Validate amount
            if (dto.Amount < config.MinWithdrawalAmount)
                throw new ArgumentException($"Withdrawal amount must be at least {config.MinWithdrawalAmount:N0} VND");

            if (dto.Amount > config.MaxWithdrawalAmount)
                throw new ArgumentException($"Withdrawal amount cannot exceed {config.MaxWithdrawalAmount:N0} VND");

            // 4. Check available balance
            var canWithdraw = await _shopWalletService.CanWithdrawAsync(dto.ShopId, dto.Amount);
            if (!canWithdraw)
            {
                var wallet = await _shopWalletService.GetOrCreateWalletAsync(dto.ShopId);
                throw new InvalidOperationException($"Insufficient available balance. Available: {wallet.AvailableBalance:N0}, Required: {dto.Amount:N0}");
            }

            // 5. Lấy hoặc tạo wallet
            var shopWallet = await _shopWalletService.GetOrCreateWalletAsync(dto.ShopId);

            // 6. Tạo WithdrawalRequest
            var withdrawalRequest = new WithdrawalRequest
            {
                Id = Guid.NewGuid().ToString(),
                ShopWalletId = shopWallet.Id,
                ShopId = dto.ShopId,
                Amount = dto.Amount,
                BankName = dto.BankName,
                BankAccountNumber = dto.BankAccountNumber,
                BankAccountName = dto.BankAccountName,
                BankBranch = dto.BankBranch,
                Status = WithdrawalStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                Note = dto.Note
            };

            await _unitOfWork.WithdrawalRequests.AddAsync(withdrawalRequest);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation(
                "Withdrawal request created: {WithdrawalId}, Shop: {ShopId}, Amount: {Amount}",
                withdrawalRequest.Id, dto.ShopId, dto.Amount);

            // TODO: Send notification cho admin

            return withdrawalRequest;
        }

        /// <summary>
        /// Admin approve withdrawal request
        /// </summary>
        public async Task<WithdrawalRequest> ApproveWithdrawalAsync(string withdrawalId, string adminId, string? note = null)
        {
            _logger.LogInformation("Admin {AdminId} approving withdrawal {WithdrawalId}", adminId, withdrawalId);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Lấy withdrawal request
                var withdrawalRequest = await _unitOfWork.WithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId);

                if (withdrawalRequest == null)
                    throw new InvalidOperationException($"Withdrawal request {withdrawalId} not found");

                if (withdrawalRequest.Status != WithdrawalStatus.Pending)
                    throw new InvalidOperationException($"Withdrawal request {withdrawalId} is not pending. Current status: {withdrawalRequest.Status}");

                // 2. Validate balance vẫn đủ
                var canWithdraw = await _shopWalletService.CanWithdrawAsync(withdrawalRequest.ShopId, withdrawalRequest.Amount);
                if (!canWithdraw)
                {
                    var shopWallet = await _shopWalletService.GetOrCreateWalletAsync(withdrawalRequest.ShopId);
                    throw new InvalidOperationException($"Insufficient balance. Available: {shopWallet.AvailableBalance}, Required: {withdrawalRequest.Amount}");
                }

                // 3. TRỪ TIỀN NGAY (lock để tránh rút 2 lần)
                await _shopWalletService.DeductBalanceAsync(
                    withdrawalRequest.ShopId,
                    withdrawalRequest.Amount,
                    WalletTransactionType.Withdrawal,
                    withdrawalRequest.Id,
                    $"Rút tiền vào tài khoản {withdrawalRequest.BankName} *{withdrawalRequest.BankAccountNumber.Substring(Math.Max(0, withdrawalRequest.BankAccountNumber.Length - 4))}");

                // 4. Update withdrawal request status
                withdrawalRequest.Status = WithdrawalStatus.Approved;
                withdrawalRequest.ApprovedBy = adminId;
                withdrawalRequest.ApprovedAt = DateTime.UtcNow;
                withdrawalRequest.AdminNote = note;

                await _unitOfWork.WithdrawalRequests.UpdateAsync(withdrawalRequest);

                // 5. Update shop wallet statistics
                var wallet = await _unitOfWork.ShopWallets.GetByShopIdAsync(withdrawalRequest.ShopId);
                if (wallet != null)
                {
                    wallet.TotalWithdrawn += withdrawalRequest.Amount;
                    await _unitOfWork.ShopWallets.UpdateAsync(wallet);
                }

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Withdrawal {WithdrawalId} approved. Amount {Amount} deducted from Shop {ShopId}",
                    withdrawalId, withdrawalRequest.Amount, withdrawalRequest.ShopId);

                // TODO: Send notification cho shop
                // TODO: Trigger background job để xử lý chuyển khoản

                return withdrawalRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving withdrawal {WithdrawalId}", withdrawalId);
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Admin reject withdrawal request
        /// </summary>
        public async Task<WithdrawalRequest> RejectWithdrawalAsync(string withdrawalId, string adminId, string reason)
        {
            _logger.LogInformation("Admin {AdminId} rejecting withdrawal {WithdrawalId}", adminId, withdrawalId);

            var withdrawalRequest = await _unitOfWork.WithdrawalRequests.GetAsync(w => w.Id == withdrawalId);

            if (withdrawalRequest == null)
                throw new InvalidOperationException($"Withdrawal request {withdrawalId} not found");

            if (withdrawalRequest.Status != WithdrawalStatus.Pending)
                throw new InvalidOperationException($"Withdrawal request {withdrawalId} is not pending");

            withdrawalRequest.Status = WithdrawalStatus.Rejected;
            withdrawalRequest.ApprovedBy = adminId;
            withdrawalRequest.ApprovedAt = DateTime.UtcNow;
            withdrawalRequest.RejectionReason = reason;

            await _unitOfWork.WithdrawalRequests.UpdateAsync(withdrawalRequest);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Withdrawal request {WithdrawalId} rejected", withdrawalId);

            // TODO: Send notification cho shop

            return withdrawalRequest;
        }

        /// <summary>
        /// Background job xử lý chuyển khoản
        /// </summary>
        public async Task ProcessApprovedWithdrawalsAsync()
        {
            _logger.LogInformation("Processing approved withdrawals...");

            var approvedWithdrawals = await _unitOfWork.WithdrawalRequests.GetApprovedRequestsAsync();

            foreach (var withdrawal in approvedWithdrawals)
            {
                await ProcessSingleWithdrawalAsync(withdrawal);
            }

            _logger.LogInformation("Processed {Count} withdrawals", approvedWithdrawals.Count());
        }

        /// <summary>
        /// Xử lý 1 withdrawal (bank transfer)
        /// </summary>
        private async Task ProcessSingleWithdrawalAsync(WithdrawalRequest withdrawal)
        {
            _logger.LogInformation("Processing withdrawal {WithdrawalId}", withdrawal.Id);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                withdrawal.Status = WithdrawalStatus.Processing;
                withdrawal.ProcessedAt = DateTime.UtcNow;
                await _unitOfWork.WithdrawalRequests.UpdateAsync(withdrawal);
                await _unitOfWork.CompleteAsync();

                // TODO: Call real bank transfer API
                // STUB: Simulate bank transfer
                bool transferSuccess = await SimulateBankTransferAsync(withdrawal);

                if (transferSuccess)
                {
                    // Success
                    withdrawal.Status = WithdrawalStatus.Completed;
                    withdrawal.CompletedAt = DateTime.UtcNow;
                    withdrawal.TransactionReference = $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}"; // Fake reference

                    _logger.LogInformation("Withdrawal {WithdrawalId} completed successfully", withdrawal.Id);

                    // TODO: Send notification cho shop
                }
                else
                {
                    // Failed → HOÀN TIỀN
                    withdrawal.Status = WithdrawalStatus.Failed;
                    withdrawal.CompletedAt = DateTime.UtcNow;
                    withdrawal.FailureReason = "Bank transfer failed (simulated)";

                    // Hoàn tiền vào AvailableBalance
                    await _shopWalletService.AddAvailableBalanceAsync(
                        withdrawal.ShopId,
                        withdrawal.Amount,
                        withdrawal.Id,
                        $"Hoàn tiền do rút tiền thất bại - {withdrawal.Id}");

                    // Trừ TotalWithdrawn
                    var wallet = await _unitOfWork.ShopWallets.GetByShopIdAsync(withdrawal.ShopId);
                    if (wallet != null)
                    {
                        wallet.TotalWithdrawn -= withdrawal.Amount;
                        await _unitOfWork.ShopWallets.UpdateAsync(wallet);
                    }

                    _logger.LogWarning("Withdrawal {WithdrawalId} failed. Amount refunded to shop wallet.", withdrawal.Id);

                    // TODO: Send notification cho shop
                }

                await _unitOfWork.WithdrawalRequests.UpdateAsync(withdrawal);
                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing withdrawal {WithdrawalId}", withdrawal.Id);

                withdrawal.Status = WithdrawalStatus.Failed;
                withdrawal.FailureReason = $"Error: {ex.Message}";
                await _unitOfWork.WithdrawalRequests.UpdateAsync(withdrawal);
                await _unitOfWork.CompleteAsync();

                await transaction.RollbackAsync();
            }
        }

        /// <summary>
        /// Simulate bank transfer (STUB)
        /// </summary>
        private async Task<bool> SimulateBankTransferAsync(WithdrawalRequest withdrawal)
        {
            // TODO: Implement real bank transfer API
            // VD: VietQR, Napas, Bank API

            _logger.LogWarning("Using STUB bank transfer. Implement real API!");

            // Simulate delay
            await Task.Delay(2000);

            // Simulate 90% success rate
            var random = new Random();
            return random.Next(100) < 90;
        }

        /// <summary>
        /// Lấy withdrawal request by ID
        /// </summary>
        public async Task<WithdrawalRequest?> GetWithdrawalRequestAsync(string withdrawalId)
        {
            return await _unitOfWork.WithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId);
        }

        /// <summary>
        /// Lấy withdrawal requests theo shop
        /// </summary>
        public async Task<IEnumerable<WithdrawalRequest>> GetWithdrawalRequestsByShopAsync(int shopId, int pageNumber = 1, int pageSize = 20)
        {
            return await _unitOfWork.WithdrawalRequests.GetByShopIdAsync(shopId, pageNumber, pageSize);
        }

        /// <summary>
        /// Lấy pending withdrawal requests
        /// </summary>
        public async Task<IEnumerable<WithdrawalRequest>> GetPendingWithdrawalRequestsAsync()
        {
            return await _unitOfWork.WithdrawalRequests.GetPendingRequestsAsync();
        }

        /// <summary>
        /// Cancel withdrawal (chỉ khi Pending)
        /// </summary>
        public async Task<WithdrawalRequest> CancelWithdrawalRequestAsync(string withdrawalId, string userId)
        {
            _logger.LogInformation("User {UserId} cancelling withdrawal {WithdrawalId}", userId, withdrawalId);

            var withdrawal = await _unitOfWork.WithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId);

            if (withdrawal == null)
                throw new InvalidOperationException($"Withdrawal request {withdrawalId} not found");

            if (withdrawal.Status != WithdrawalStatus.Pending)
                throw new InvalidOperationException($"Cannot cancel withdrawal with status {withdrawal.Status}");

            // Verify ownership
            if (withdrawal.Shop.SellerId != userId)
                throw new UnauthorizedAccessException("You can only cancel your own withdrawal requests");

            withdrawal.Status = WithdrawalStatus.Rejected;
            withdrawal.RejectionReason = "Cancelled by user";
            withdrawal.ApprovedAt = DateTime.UtcNow;

            await _unitOfWork.WithdrawalRequests.UpdateAsync(withdrawal);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Withdrawal {WithdrawalId} cancelled by user", withdrawalId);

            return withdrawal;
        }
    }
}