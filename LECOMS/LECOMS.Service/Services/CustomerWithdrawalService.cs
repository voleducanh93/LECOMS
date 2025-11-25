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
        /// Customer tạo Yêu cầu rút tiền
        /// </summary>
        public async Task<CustomerWithdrawalRequest> CreateCustomerWithdrawalRequestAsync(CreateCustomerWithdrawalRequestDto dto)
        {
            _logger.LogInformation("Tạo yêu cầu rút tiền của khách hàng cho Khách hàng {CustomerId}, Số lượng: {Amount}", dto.CustomerId, dto.Amount);

            // 1. Validate customer
            var customer = await _unitOfWork.Users.GetUserByIdAsync(dto.CustomerId);
            if (customer == null)
                throw new InvalidOperationException($"Khách hàng {dto.CustomerId} không tìm thấy");

            // 2. Lấy platform config
            var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

            // 3. Validate amount
            if (dto.Amount < config.MinWithdrawalAmount)
                throw new ArgumentException($"Số tiền rút ít nhất phải là {config.MinWithdrawalAmount:N0} VND");

            if (dto.Amount > config.MaxWithdrawalAmount)
                throw new ArgumentException($"Số tiền rút không thể vượt quá {config.MaxWithdrawalAmount:N0} VND");

            // 4. Check balance
            var hasSufficientBalance = await _customerWalletService.HasSufficientBalanceAsync(dto.CustomerId, dto.Amount);
            if (!hasSufficientBalance)
            {
                var balance = await _customerWalletService.GetBalanceAsync(dto.CustomerId);
                throw new InvalidOperationException($"Không đủ cân bằng. Có sẵn: {balance:N0}, Yêu cầu: {dto.Amount:N0}");
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
                "Yêu cầu rút tiền của khách hàng đã được tạo: {WithdrawalId}, Khách hàng: {CustomerId}, Số lượng: {Amount}",
                withdrawalRequest.Id, dto.CustomerId, dto.Amount);

            // TODO: Send notification cho admin

            return withdrawalRequest;
        }

        /// <summary>
        /// Admin approve customer Yêu cầu rút tiền
        /// </summary>
        public async Task<CustomerWithdrawalRequest> ApproveCustomerWithdrawalAsync(string withdrawalId, string adminId, string? note = null)
        {
            _logger.LogInformation("Admin {AdminId} phê duyệt việc rút tiền của khách hàng {WithdrawalId}", adminId, withdrawalId);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Lấy Yêu cầu rút tiền
                var withdrawalRequest = await _unitOfWork.CustomerWithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId);

                if (withdrawalRequest == null)
                    throw new InvalidOperationException($"Yêu cầu rút tiền của khách hàng {withdrawalId} không tìm thấy");

                if (withdrawalRequest.Status != WithdrawalStatus.Pending)
                    throw new InvalidOperationException($"Yêu cầu rút tiền của khách hàng {withdrawalId} không đang chờ xử lý. Tình trạng hiện tại: {withdrawalRequest.Status}");

                // 2. Validate balance vẫn đủ
                var hasSufficientBalance = await _customerWalletService.HasSufficientBalanceAsync(
                    withdrawalRequest.CustomerId,
                    withdrawalRequest.Amount);

                if (!hasSufficientBalance)
                {
                    var balance = await _customerWalletService.GetBalanceAsync(withdrawalRequest.CustomerId);
                    throw new InvalidOperationException($"Không đủ cân bằng. Có sẵn: {balance}, Yêu cầu: {withdrawalRequest.Amount}");
                }

                // 3. TRỪ TIỀN NGAY (lock)
                await _customerWalletService.DeductBalanceAsync(
                    withdrawalRequest.CustomerId,
                    withdrawalRequest.Amount,
                    WalletTransactionType.Withdrawal,
                    withdrawalRequest.Id,
                    $"Rút tiền vào tài khoản {withdrawalRequest.BankName} *{withdrawalRequest.BankAccountNumber.Substring(Math.Max(0, withdrawalRequest.BankAccountNumber.Length - 4))}");

                // 4. Update Yêu cầu rút tiền status
                withdrawalRequest.Status = WithdrawalStatus.Approved;
                withdrawalRequest.ApprovedBy = adminId;
                withdrawalRequest.ApprovedAt = DateTime.UtcNow;
                withdrawalRequest.AdminNote = note;

                await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawalRequest);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Khách hàng rút tiền {WithdrawalId} tán thành. Số lượng {Amount} được khấu trừ từ Khách hàng {CustomerId}",
                    withdrawalId, withdrawalRequest.Amount, withdrawalRequest.CustomerId);

                // TODO: Send notification cho customer
                // TODO: Trigger background job để xử lý chuyển khoản

                return withdrawalRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi phê duyệt rút tiền của khách hàng {WithdrawalId}", withdrawalId);
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Admin reject customer Yêu cầu rút tiền
        /// </summary>
        public async Task<CustomerWithdrawalRequest> RejectCustomerWithdrawalAsync(string withdrawalId, string adminId, string reason)
        {
            _logger.LogInformation("Admin {AdminId} từ chối rút tiền của khách hàng {WithdrawalId}", adminId, withdrawalId);

            var withdrawalRequest = await _unitOfWork.CustomerWithdrawalRequests.GetAsync(w => w.Id == withdrawalId);

            if (withdrawalRequest == null)
                throw new InvalidOperationException($"Yêu cầu rút tiền của khách hàng {withdrawalId} không tìm thấy");

            if (withdrawalRequest.Status != WithdrawalStatus.Pending)
                throw new InvalidOperationException($"Yêu cầu rút tiền của khách hàng {withdrawalId} không đang chờ xử lý");

            withdrawalRequest.Status = WithdrawalStatus.Rejected;
            withdrawalRequest.ApprovedBy = adminId;
            withdrawalRequest.ApprovedAt = DateTime.UtcNow;
            withdrawalRequest.RejectionReason = reason;

            await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawalRequest);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Yêu cầu rút tiền của khách hàng {WithdrawalId} bị loại bỏ", withdrawalId);

            // TODO: Send notification cho customer

            return withdrawalRequest;
        }

        /// <summary>
        /// Background job xử lý chuyển khoản cho customers
        /// </summary>
        public async Task ProcessApprovedCustomerWithdrawalsAsync()
        {
            _logger.LogInformation("Xử lý việc rút tiền của khách hàng đã được phê duyệt...");

            var approvedWithdrawals = await _unitOfWork.CustomerWithdrawalRequests.GetApprovedRequestsAsync();

            foreach (var withdrawal in approvedWithdrawals)
            {
                await ProcessSingleCustomerWithdrawalAsync(withdrawal);
            }

            _logger.LogInformation("Đã xử lý {Count} rút tiền của khách hàng", approvedWithdrawals.Count());
        }

        /// <summary>
        /// Xử lý 1 customer withdrawal (bank transfer)
        /// </summary>
        private async Task ProcessSingleCustomerWithdrawalAsync(CustomerWithdrawalRequest withdrawal)
        {
            _logger.LogInformation("Xử lý việc rút tiền của khách hàng {WithdrawalId}", withdrawal.Id);

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

                    _logger.LogInformation("Khách hàng rút tiền {WithdrawalId} hoàn thành thành công", withdrawal.Id);

                    // TODO: Send notification cho customer
                }
                else
                {
                    // Failed → HOÀN TIỀN
                    withdrawal.Status = WithdrawalStatus.Failed;
                    withdrawal.CompletedAt = DateTime.UtcNow;
                    withdrawal.FailureReason = "Chuyển khoản ngân hàng không thành công (simulated)";

                    // Hoàn tiền vào CustomerWallet
                    await _customerWalletService.AddBalanceAsync(
                        withdrawal.CustomerId,
                        withdrawal.Amount,
                        withdrawal.Id,
                        $"Hoàn tiền do rút tiền thất bại - {withdrawal.Id}");

                    _logger.LogWarning(
                        "Khách hàng rút tiền {WithdrawalId} thất bại. Số tiền được hoàn lại vào ví của khách hàng.",
                        withdrawal.Id);

                    // TODO: Send notification cho customer
                }

                await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawal);
                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý việc rút tiền của khách hàng {WithdrawalId}", withdrawal.Id);

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

            _logger.LogWarning("Sử dụng chuyển khoản ngân hàng STUB cho khách hàng. Triển khai API thực sự!");

            // Simulate delay
            await Task.Delay(2000);

            // Simulate 90% success rate
            var random = new Random();
            return random.Next(100) < 90;
        }

        /// <summary>
        /// Lấy customer Yêu cầu rút tiền by ID
        /// </summary>
        public async Task<CustomerWithdrawalRequest?> GetCustomerWithdrawalRequestAsync(string withdrawalId)
        {
            return await _unitOfWork.CustomerWithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId);
        }

        /// <summary>
        /// Lấy Yêu cầu rút tiền theo customer
        /// </summary>
        public async Task<IEnumerable<CustomerWithdrawalRequest>> GetCustomerWithdrawalRequestsByCustomerAsync(
            string customerId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await _unitOfWork.CustomerWithdrawalRequests.GetByCustomerIdAsync(customerId, pageNumber, pageSize);
        }

        /// <summary>
        /// Lấy pending customer Yêu cầu rút tiền
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
            _logger.LogInformation("Khách hàng {CustomerId} hủy rút tiền {WithdrawalId}", customerId, withdrawalId);

            var withdrawal = await _unitOfWork.CustomerWithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId);

            if (withdrawal == null)
                throw new InvalidOperationException($"Yêu cầu rút tiền của khách hàng {withdrawalId} không tìm thấy");

            if (withdrawal.Status != WithdrawalStatus.Pending)
                throw new InvalidOperationException($"Không thể hủy rút tiền với trạng thái {withdrawal.Status}");

            // Verify ownership
            if (withdrawal.CustomerId != customerId)
                throw new UnauthorizedAccessException("Bạn chỉ có thể hủy yêu cầu rút tiền của chính mình");

            withdrawal.Status = WithdrawalStatus.Rejected;
            withdrawal.RejectionReason = "Bị khách hàng hủy";
            withdrawal.ApprovedAt = DateTime.UtcNow;

            await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawal);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Khách hàng rút tiền {WithdrawalId} bị khách hàng hủy", withdrawalId);

            return withdrawal;
        }
    }
}