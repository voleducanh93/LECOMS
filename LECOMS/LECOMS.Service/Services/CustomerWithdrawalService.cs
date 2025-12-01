using AutoMapper;
using LECOMS.Data.DTOs.Withdrawal;
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
        private readonly IMapper _mapper;

        public CustomerWithdrawalService(
            IUnitOfWork unitOfWork,
            ICustomerWalletService customerWalletService,
            ILogger<CustomerWithdrawalService> logger, 
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _customerWalletService = customerWalletService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<CustomerWithdrawalRequest> CreateCustomerWithdrawalRequestAsync(CreateCustomerWithdrawalRequestDto dto)
        {
            var customer = await _unitOfWork.Users.GetUserByIdAsync(dto.CustomerId)
                           ?? throw new InvalidOperationException("Khách hàng không tồn tại");

            var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

            if (dto.Amount < config.MinWithdrawalAmount)
                throw new ArgumentException($"Số tiền rút ít nhất {config.MinWithdrawalAmount:N0} VND");

            if (dto.Amount > config.MaxWithdrawalAmount)
                throw new ArgumentException($"Số tiền rút tối đa {config.MaxWithdrawalAmount:N0} VND");

            var hasBalance = await _customerWalletService.HasSufficientBalanceAsync(dto.CustomerId, dto.Amount);
            if (!hasBalance)
            {
                var balance = await _customerWalletService.GetBalanceAsync(dto.CustomerId);
                throw new InvalidOperationException($"Số dư không đủ. Có: {balance:N0}, Yêu cầu: {dto.Amount:N0}");
            }

            var wallet = await _customerWalletService.GetOrCreateWalletAsync(dto.CustomerId);

            var request = new CustomerWithdrawalRequest
            {
                CustomerId = dto.CustomerId,
                CustomerWalletId = wallet.Id,
                Amount = dto.Amount,
                BankName = dto.BankName,
                BankAccountNumber = dto.BankAccountNumber,
                BankAccountName = dto.BankAccountName,
                BankBranch = dto.BankBranch,
                Status = WithdrawalStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                Note = dto.Note
            };

            await _unitOfWork.CustomerWithdrawalRequests.AddAsync(request);
            await _unitOfWork.CompleteAsync();

            return request;
        }

        public Task<IEnumerable<CustomerWithdrawalRequest>> GetCustomerWithdrawalRequestsByCustomerAsync(
            string customerId, int pageNumber, int pageSize)
            => _unitOfWork.CustomerWithdrawalRequests.GetByCustomerIdAsync(customerId, pageNumber, pageSize);

        public Task<IEnumerable<CustomerWithdrawalRequest>> GetPendingCustomerWithdrawalRequestsAsync()
            => _unitOfWork.CustomerWithdrawalRequests.GetPendingAsync();

        public Task<CustomerWithdrawalRequest?> GetCustomerWithdrawalRequestAsync(string withdrawalId)
            => _unitOfWork.CustomerWithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId);

        public async Task<CustomerWithdrawalRequest> ApproveCustomerWithdrawalAsync(string withdrawalId, string adminId, string? note)
        {
            var withdrawal = await _unitOfWork.CustomerWithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId)
                            ?? throw new InvalidOperationException("Yêu cầu không tìm thấy");

            if (withdrawal.Status != WithdrawalStatus.Pending)
                throw new InvalidOperationException("Chỉ approve khi Pending");

            var hasBalance = await _customerWalletService.HasSufficientBalanceAsync(withdrawal.CustomerId, withdrawal.Amount);
            if (!hasBalance)
                throw new InvalidOperationException("Số dư không đủ để rút");

            await _customerWalletService.DeductBalanceAsync(
                withdrawal.CustomerId,
                withdrawal.Amount,
                WalletTransactionType.Withdrawal,
                withdrawal.Id,
                $"Rút tiền vào tài khoản {withdrawal.BankName}");

            withdrawal.Status = WithdrawalStatus.Approved;
            withdrawal.ApprovedBy = adminId;
            withdrawal.ApprovedAt = DateTime.UtcNow;
            withdrawal.AdminNote = note;

            await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawal);
            await _unitOfWork.CompleteAsync();

            return withdrawal;
        }

        public async Task<CustomerWithdrawalRequest> CompleteCustomerWithdrawalAsync(string withdrawalId, string adminId)
        {
            var withdrawal = await _unitOfWork.CustomerWithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId)
                            ?? throw new InvalidOperationException("Yêu cầu không tìm thấy");

            if (withdrawal.Status != WithdrawalStatus.Approved)
                throw new InvalidOperationException("Chỉ complete khi Approved");

            withdrawal.Status = WithdrawalStatus.Completed;
            withdrawal.CompletedAt = DateTime.UtcNow;

            await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawal);
            await _unitOfWork.CompleteAsync();

            return withdrawal;
        }

        public async Task<CustomerWithdrawalRequest> RejectCustomerWithdrawalAsync(string withdrawalId, string adminId, string reason)
        {
            var withdrawal = await _unitOfWork.CustomerWithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId)
                            ?? throw new InvalidOperationException("Yêu cầu không tìm thấy");

            if (withdrawal.Status != WithdrawalStatus.Pending)
                throw new InvalidOperationException("Chỉ reject được khi Pending");

            withdrawal.Status = WithdrawalStatus.Rejected;
            withdrawal.RejectionReason = reason;
            withdrawal.ApprovedBy = adminId;
            withdrawal.ApprovedAt = DateTime.UtcNow;

            await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawal);
            await _unitOfWork.CompleteAsync();

            return withdrawal;
        }

        public async Task<CustomerWithdrawalRequest> CancelCustomerWithdrawalRequestAsync(string withdrawalId, string customerId)
        {
            var withdrawal = await _unitOfWork.CustomerWithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId)
                            ?? throw new InvalidOperationException("Yêu cầu không tìm thấy");

            if (withdrawal.Status != WithdrawalStatus.Pending)
                throw new InvalidOperationException("Chỉ cancel được khi Pending");

            if (withdrawal.CustomerId != customerId)
                throw new UnauthorizedAccessException("Bạn không có quyền hủy yêu cầu này");

            withdrawal.Status = WithdrawalStatus.Rejected;
            withdrawal.RejectionReason = "Khách hàng tự hủy";
            withdrawal.ApprovedAt = DateTime.UtcNow;

            await _unitOfWork.CustomerWithdrawalRequests.UpdateAsync(withdrawal);
            await _unitOfWork.CompleteAsync();

            return withdrawal;
        }

        public async Task<CustomerWithdrawalDetailDTO?> GetByIdAsync(string id)
        {
            var entity = await _unitOfWork.CustomerWithdrawalRequests.GetByIdWithDetailsAsync(id);
            if (entity == null) return null;

            return _mapper.Map<CustomerWithdrawalDetailDTO>(entity);
        }

    }
}