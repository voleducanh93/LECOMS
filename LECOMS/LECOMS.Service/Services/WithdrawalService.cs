using AutoMapper;
using LECOMS.Data.DTOs.Withdrawal;
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
    public class WithdrawalService : IWithdrawalService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IShopWalletService _shopWalletService;
        private readonly ILogger<WithdrawalService> _logger;
        private readonly IMapper _mapper;

        public WithdrawalService(
            IUnitOfWork unitOfWork,
            IShopWalletService shopWalletService,
            ILogger<WithdrawalService> logger,
             IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _shopWalletService = shopWalletService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<WithdrawalRequest> CreateWithdrawalRequestAsync(CreateWithdrawalRequestDto dto)
        {
            _logger.LogInformation("Creating withdrawal for Shop {ShopId}, Amount {Amount}", dto.ShopId, dto.Amount);

            var shop = await _unitOfWork.Shops.GetAsync(s => s.Id == dto.ShopId)
                       ?? throw new InvalidOperationException($"Shop {dto.ShopId} không tìm thấy");

            var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

            if (dto.Amount < config.MinWithdrawalAmount)
                throw new ArgumentException($"Số tiền rút ít nhất {config.MinWithdrawalAmount:N0} VND");

            if (dto.Amount > config.MaxWithdrawalAmount)
                throw new ArgumentException($"Số tiền rút tối đa {config.MaxWithdrawalAmount:N0} VND");

            var canWithdraw = await _shopWalletService.CanWithdrawAsync(dto.ShopId, dto.Amount);
            if (!canWithdraw)
            {
                var wallet = await _shopWalletService.GetOrCreateWalletAsync(dto.ShopId);
                throw new InvalidOperationException($"Số dư khả dụng không đủ.  Có: {wallet.AvailableBalance:N0}, Yêu cầu: {dto.Amount:N0}");
            }

            var shopWallet = await _shopWalletService.GetOrCreateWalletAsync(dto.ShopId);

            var request = new WithdrawalRequest
            {
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

            await _unitOfWork.WithdrawalRequests.AddAsync(request);

            // ⭐ TRỪ TIỀN NGAY KHI TẠO YÊU CẦU (khỏi AvailableBalance)
            await _shopWalletService.DeductBalanceAsync(
                dto.ShopId,
                dto.Amount,
                WalletTransactionType.Withdrawal,
                request.Id,
                $"Giữ tiền cho yêu cầu rút tiền vào {dto.BankName}");

            await _unitOfWork.CompleteAsync();

            return request;
        }

        public Task<IEnumerable<WithdrawalRequest>> GetWithdrawalRequestsByShopAsync(int shopId, int pageNumber, int pageSize)
            => _unitOfWork.WithdrawalRequests.GetByShopIdAsync(shopId, pageNumber, pageSize);

        public Task<IEnumerable<WithdrawalRequest>> GetPendingWithdrawalRequestsAsync()
            => _unitOfWork.WithdrawalRequests.GetPendingAsync();

        public Task<WithdrawalRequest?> GetWithdrawalRequestAsync(string withdrawalId)
            => _unitOfWork.WithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId);

        public async Task<WithdrawalRequest> ApproveWithdrawalAsync(string withdrawalId, string adminId, string? note)
        {
            var withdrawal = await _unitOfWork.WithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId)
                            ?? throw new InvalidOperationException("Yêu cầu rút tiền không tìm thấy");

            if (withdrawal.Status != WithdrawalStatus.Pending)
                throw new InvalidOperationException("Chỉ có thể approve khi Pending");

            var canWithdraw = await _shopWalletService.CanWithdrawAsync(withdrawal.ShopId, withdrawal.Amount);
            if (!canWithdraw)
                throw new InvalidOperationException("Số dư không đủ để rút");

            //await _shopWalletService.DeductBalanceAsync(
            //    withdrawal.ShopId,
            //    withdrawal.Amount,
            //    WalletTransactionType.Withdrawal,
            //    withdrawal.Id,
            //    $"Rút tiền vào tài khoản {withdrawal.BankName}");

            withdrawal.Status = WithdrawalStatus.Approved;
            withdrawal.ApprovedBy = adminId;
            withdrawal.ApprovedAt = DateTime.UtcNow;
            withdrawal.AdminNote = note;

            await _unitOfWork.WithdrawalRequests.UpdateAsync(withdrawal);
            await _unitOfWork.CompleteAsync();

            return withdrawal;
        }

        public async Task<WithdrawalRequest> CompleteWithdrawalAsync(string withdrawalId, string adminId)
        {
            var withdrawal = await _unitOfWork.WithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId)
                            ?? throw new InvalidOperationException("Yêu cầu rút tiền không tìm thấy");

            if (withdrawal.Status != WithdrawalStatus.Approved)
                throw new InvalidOperationException("Chỉ complete được khi đã Approved");

            // ⭐ CẬP NHẬT TotalWithdrawn khi hoàn tất rút tiền
            var wallet = await _shopWalletService.GetOrCreateWalletAsync(withdrawal.ShopId);
            wallet.TotalWithdrawn += withdrawal.Amount;
            wallet.LastUpdated = DateTime.UtcNow;
            await _unitOfWork.ShopWallets.UpdateAsync(wallet);

            // Cập nhật status của withdrawal request
            withdrawal.Status = WithdrawalStatus.Completed;
            withdrawal.CompletedAt = DateTime.UtcNow;

            await _unitOfWork.WithdrawalRequests.UpdateAsync(withdrawal);
            await _unitOfWork.CompleteAsync();

            return withdrawal;
        }

        public async Task<WithdrawalRequest> RejectWithdrawalAsync(string withdrawalId, string adminId, string reason)
        {
            var withdrawal = await _unitOfWork.WithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId)
                            ?? throw new InvalidOperationException("Yêu cầu rút tiền không tìm thấy");

            if (withdrawal.Status != WithdrawalStatus.Pending)
                throw new InvalidOperationException("Chỉ reject được khi Pending");

            withdrawal.Status = WithdrawalStatus.Rejected;
            withdrawal.RejectionReason = reason;
            withdrawal.ApprovedBy = adminId;
            withdrawal.ApprovedAt = DateTime.UtcNow;

            await _unitOfWork.WithdrawalRequests.UpdateAsync(withdrawal);
            await _unitOfWork.CompleteAsync();

            return withdrawal;
        }

        public async Task<WithdrawalRequest> CancelWithdrawalRequestAsync(string withdrawalId, string sellerUserId)
        {
            var withdrawal = await _unitOfWork.WithdrawalRequests.GetByIdWithDetailsAsync(withdrawalId)
                            ?? throw new InvalidOperationException("Yêu cầu rút tiền không tìm thấy");

            if (withdrawal.Status != WithdrawalStatus.Pending)
                throw new InvalidOperationException("Chỉ cancel được khi Pending");

            if (withdrawal.Shop.SellerId != sellerUserId)
                throw new UnauthorizedAccessException("Bạn không có quyền hủy yêu cầu này");

            await _shopWalletService.AddAvailableBalanceAsync(
                withdrawal.ShopId,
                withdrawal.Amount,
                withdrawal.Id,
               "Hoàn tiền do người bán tự hủy yêu cầu rút tiền");

            withdrawal.Status = WithdrawalStatus.Rejected;
            withdrawal.RejectionReason = "Người bán tự hủy";
            withdrawal.ApprovedAt = DateTime.UtcNow;

            await _unitOfWork.WithdrawalRequests.UpdateAsync(withdrawal);
            await _unitOfWork.CompleteAsync();

            return withdrawal;
        }

        public async Task<ShopWithdrawalDetailDTO?> GetByIdAsync(string id)
        {
            var entity = await _unitOfWork.WithdrawalRequests.GetByIdWithDetailsAsync(id);
            if (entity == null) return null;

            return _mapper.Map<ShopWithdrawalDetailDTO>(entity);
        }

    }
}