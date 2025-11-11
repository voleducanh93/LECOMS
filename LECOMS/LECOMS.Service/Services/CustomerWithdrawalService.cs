using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    /// <summary>
    /// Service implementation cho CustomerWithdrawal
    /// WithdrawalService nhưng cho CustomerWallet
    /// </summary>
    public class CustomerWithdrawalService : ICustomerWithdrawalService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICustomerWalletService _customerWalletService;
        private readonly ILogger<CustomerWithdrawalService> _logger;

        public CustomerWithdrawalService(
            IUnitOfWork unitOfWork,
            ICustomerWalletService customerWalletService,
            ILogger<CustomerWithdrawalService> logger)
        {
            _unitOfWork = unitOfWork;
            _customerWalletService = customerWalletService;
            _logger = logger;
        }

        /// <summary>
        /// Customer tạo withdrawal request
        /// </summary>
        public async Task<CustomerWithdrawalRequest> CreateCustomerWithdrawalRequestAsync(CreateCustomerWithdrawalRequestDto dto)
        {
            _logger.LogInformation("Creating customer withdrawal request for Customer {CustomerId}, Amount: {Amount}", dto.CustomerId, dto.Amount);

            // 1. Validate customer
            var customer = await _unitOfWork.Users.GetUserByIdAsync(dto.CustomerId);
            if (customer == null)
                throw new InvalidOperationException($"Customer {dto.CustomerId} not found");

            // 2. Lấy platform config
            var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

            // 3. Validate amount
            if (dto.Amount < config.MinWithdrawalAmount)
                throw new ArgumentException($"Withdrawal amount must be at least {config.MinWithdrawalAmount:N0} VND");

            if (dto.Amount > config.MaxWithdrawalAmount)
                throw new ArgumentException($"Withdrawal amount cannot exceed {config.MaxWithdrawalAmount:N0} VND");

            // 4. Check balance
            var hasSufficientBalance = await _customerWalletService.HasSufficientBalanceAsync(dto.CustomerId, dto.Amount);
            if (!hasSufficientBalance)
            {
                var balance = await _customerWalletService.GetBalanceAsync(dto.CustomerId);
                throw new InvalidOperationException($"Insufficient balance. Available: {balance:N0}, Required: {dto.Amount:N0}");
            }

            // 5. Lấy hoặc tạo wallet
            var customerWallet = await _customerWalletService.GetOrCreateWalletAsync(dto.CustomerId);

            // 6. Tạo CustomerWithdrawalRequest
            var withdrawalRequest = new CustomerWithdrawalRequest
            {
                Id = Guid.NewGuid().ToString(),
                CustomerWalletId = customerWallet.Id,
                CustomerId = dto.CustomerId,
                Amount = dto.Amount,
                BankName = dto.BankName,
                BankAccountNumber = dto.BankAccountNumber,
                BankAccountName = dto.BankAccountName,
                BankBranch = dto.BankBranch,
                Status = WithdrawalStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                Note = dto.Note
            };

            await _unitOfWork.CustomerWithdrawalRequests.AddAsync(withdrawalRequest);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation(
                "Customer withdrawal request created: {WithdrawalId}, Customer: {CustomerId}, Amount: {Amount}",
                withdrawalRequest.Id, dto.CustomerId, dto.Amount);

            // TODO: Send notification cho admin

            return withdrawalRequest;
        }

        /// <summary>
        /// Admin approve customer withdrawal request
        /// </summary>
        public async Task<CustomerWithdrawalRequest> ApproveCustomerWithdrawalAsync(string withdrawalId, string adminId, string? note = null)
        {
            _logger.LogInformation("Admin {AdminId} approving customer withdrawal {WithdrawalId}", adminId, withdrawalId);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Lấy withdrawal request
                var withdrawalRequest = await _unitOfWork.CustomerWithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId);

                if (withdrawalRequest == null)
                    throw new InvalidOperationException($"Customer withdrawal request {withdrawalId} not found");

                if (withdrawalRequest.Status != WithdrawalStatus.Pending)
                    throw new InvalidOperationException($"Customer withdrawal request {withdrawalId} is not pending. Current status: {withdrawalRequest.Status}");

                // 2. Validate balance vẫn đủ
                var hasSufficientBalance = await _customerWalletService.HasSufficientBalanceAsync(
                    withdrawalRequest.CustomerId,
                    withdrawalRequest.Amount);

                if (!hasSufficientBalance)
                {
                    var balance = await _customerWalletService.GetBalanceAsync(withdrawalRequest.CustomerId);
                    throw new InvalidOperationException($"Insufficient balance. Available: {balance}, Required: {withdrawalRequest.Amount}");
                }

                // 3. TRỪ TIỀN NGAY (lock)
                await _customerWalletService.DeductBalanceAsync(
                    withdrawalRequest.CustomerId,
                    withdrawalRequest.Amount,
                    WalletTransactionType.Withdrawal,
                    withdrawalRequest.Id,
                    $"Rút tiền vào tài khoản {withdrawalRequest.BankName} *{withdrawalRequest.BankAccountNumber.Substring(Math.Max(0, withdrawalRequest.BankAccountNumber.Length - 4))}");

                // 4. Update withdrawal request status
                withdrawalRequest.Status = WithdrawalStatus.Approved;
                withdrawalRequest.ApprovedBy = adminId;
                withdrawalRequest.ApprovedAt = DateTime.UtcNow;
                withdrawalRequest.AdminNote = note;

                await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawalRequest);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Customer withdrawal {WithdrawalId} approved. Amount {Amount} deducted from Customer {CustomerId}",
                    withdrawalId, withdrawalRequest.Amount, withdrawalRequest.CustomerId);

                // TODO: Send notification cho customer
                // TODO: Trigger background job để xử lý chuyển khoản

                return withdrawalRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving customer withdrawal {WithdrawalId}", withdrawalId);
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Admin reject customer withdrawal request
        /// </summary>
        public async Task<CustomerWithdrawalRequest> RejectCustomerWithdrawalAsync(string withdrawalId, string adminId, string reason)
        {
            _logger.LogInformation("Admin {AdminId} rejecting customer withdrawal {WithdrawalId}", adminId, withdrawalId);

            var withdrawalRequest = await _unitOfWork.CustomerWithdrawalRequests.GetAsync(w => w.Id == withdrawalId);

            if (withdrawalRequest == null)
                throw new InvalidOperationException($"Customer withdrawal request {withdrawalId} not found");

            if (withdrawalRequest.Status != WithdrawalStatus.Pending)
                throw new InvalidOperationException($"Customer withdrawal request {withdrawalId} is not pending");

            withdrawalRequest.Status = WithdrawalStatus.Rejected;
            withdrawalRequest.ApprovedBy = adminId;
            withdrawalRequest.ApprovedAt = DateTime.UtcNow;
            withdrawalRequest.RejectionReason = reason;

            await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawalRequest);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Customer withdrawal request {WithdrawalId} rejected", withdrawalId);

            // TODO: Send notification cho customer

            return withdrawalRequest;
        }

        /// <summary>
        /// Background job xử lý chuyển khoản cho customers
        /// </summary>
        public async Task ProcessApprovedCustomerWithdrawalsAsync()
        {
            _logger.LogInformation("Processing approved customer withdrawals...");

            var approvedWithdrawals = await _unitOfWork.CustomerWithdrawalRequests.GetApprovedRequestsAsync();

            foreach (var withdrawal in approvedWithdrawals)
            {
                await ProcessSingleCustomerWithdrawalAsync(withdrawal);
            }

            _logger.LogInformation("Processed {Count} customer withdrawals", approvedWithdrawals.Count());
        }

        /// <summary>
        /// Xử lý 1 customer withdrawal (bank transfer)
        /// </summary>
        private async Task ProcessSingleCustomerWithdrawalAsync(CustomerWithdrawalRequest withdrawal)
        {
            _logger.LogInformation("Processing customer withdrawal {WithdrawalId}", withdrawal.Id);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                withdrawal.Status = WithdrawalStatus.Processing;
                withdrawal.ProcessedAt = DateTime.UtcNow;
                await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawal);
                await _unitOfWork.CompleteAsync();

                // TODO: Call real bank transfer API
                // STUB: Simulate bank transfer
                bool transferSuccess = await SimulateBankTransferAsync(withdrawal);

                if (transferSuccess)
                {
                    // Success
                    withdrawal.Status = WithdrawalStatus.Completed;
                    withdrawal.CompletedAt = DateTime.UtcNow;
                    withdrawal.TransactionReference = $"CUST_TXN{DateTime.UtcNow:yyyyMMddHHmmss}"; // Fake reference

                    _logger.LogInformation("Customer withdrawal {WithdrawalId} completed successfully", withdrawal.Id);

                    // TODO: Send notification cho customer
                }
                else
                {
                    // Failed → HOÀN TIỀN
                    withdrawal.Status = WithdrawalStatus.Failed;
                    withdrawal.CompletedAt = DateTime.UtcNow;
                    withdrawal.FailureReason = "Bank transfer failed (simulated)";

                    // Hoàn tiền vào CustomerWallet
                    await _customerWalletService.AddBalanceAsync(
                        withdrawal.CustomerId,
                        withdrawal.Amount,
                        withdrawal.Id,
                        $"Hoàn tiền do rút tiền thất bại - {withdrawal.Id}");

                    _logger.LogWarning(
                        "Customer withdrawal {WithdrawalId} failed. Amount refunded to customer wallet.",
                        withdrawal.Id);

                    // TODO: Send notification cho customer
                }

                await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawal);
                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing customer withdrawal {WithdrawalId}", withdrawal.Id);

                withdrawal.Status = WithdrawalStatus.Failed;
                withdrawal.FailureReason = $"Error: {ex.Message}";
                await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawal);
                await _unitOfWork.CompleteAsync();

                await transaction.RollbackAsync();
            }
        }

        /// <summary>
        /// Simulate bank transfer (STUB)
        /// </summary>
        private async Task<bool> SimulateBankTransferAsync(CustomerWithdrawalRequest withdrawal)
        {
            // TODO: Implement real bank transfer API
            // VD: VietQR, Napas, Bank API

            _logger.LogWarning("Using STUB bank transfer for customer. Implement real API!");

            // Simulate delay
            await Task.Delay(2000);

            // Simulate 90% success rate
            var random = new Random();
            return random.Next(100) < 90;
        }

        /// <summary>
        /// Lấy customer withdrawal request by ID
        /// </summary>
        public async Task<CustomerWithdrawalRequest?> GetCustomerWithdrawalRequestAsync(string withdrawalId)
        {
            return await _unitOfWork.CustomerWithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId);
        }

        /// <summary>
        /// Lấy withdrawal requests theo customer
        /// </summary>
        public async Task<IEnumerable<CustomerWithdrawalRequest>> GetCustomerWithdrawalRequestsByCustomerAsync(
            string customerId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await _unitOfWork.CustomerWithdrawalRequests.GetByCustomerIdAsync(customerId, pageNumber, pageSize);
        }

        /// <summary>
        /// Lấy pending customer withdrawal requests
        /// </summary>
        public async Task<IEnumerable<CustomerWithdrawalRequest>> GetPendingCustomerWithdrawalRequestsAsync()
        {
            return await _unitOfWork.CustomerWithdrawalRequests.GetPendingRequestsAsync();
        }

        /// <summary>
        /// Cancel customer withdrawal (chỉ khi Pending)
        /// </summary>
        public async Task<CustomerWithdrawalRequest> CancelCustomerWithdrawalRequestAsync(string withdrawalId, string customerId)
        {
            _logger.LogInformation("Customer {CustomerId} cancelling withdrawal {WithdrawalId}", customerId, withdrawalId);

            var withdrawal = await _unitOfWork.CustomerWithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId);

            if (withdrawal == null)
                throw new InvalidOperationException($"Customer withdrawal request {withdrawalId} not found");

            if (withdrawal.Status != WithdrawalStatus.Pending)
                throw new InvalidOperationException($"Cannot cancel withdrawal with status {withdrawal.Status}");

            // Verify ownership
            if (withdrawal.CustomerId != customerId)
                throw new UnauthorizedAccessException("You can only cancel your own withdrawal requests");

            withdrawal.Status = WithdrawalStatus.Rejected;
            withdrawal.RejectionReason = "Cancelled by customer";
            withdrawal.ApprovedAt = DateTime.UtcNow;

            await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawal);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Customer withdrawal {WithdrawalId} cancelled by customer", withdrawalId);

            return withdrawal;
        }
    }
}