using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    /// <summary>
    /// Service xử lý rút tiền từ ShopWallet
    /// </summary>
    public interface IWithdrawalService
    {
        /// <summary>
        /// Shop tạo withdrawal request
        /// 
        /// FLOW:
        /// 1. Validate amount (min, max từ PlatformConfig)
        /// 2. Validate AvailableBalance >= amount
        /// 3. Validate bank info
        /// 4. Tạo WithdrawalRequest (Status = Pending)
        /// 5. Send notification cho admin
        /// </summary>
        Task<WithdrawalRequest> CreateWithdrawalRequestAsync(CreateWithdrawalRequestDto dto);

        /// <summary>
        /// Admin approve withdrawal request
        /// 
        /// FLOW:
        /// 1. Validate AvailableBalance vẫn đủ
        /// 2. TRỪ TIỀN khỏi AvailableBalance NGAY (lock)
        /// 3. Update WithdrawalRequest.Status = Approved
        /// 4. Update TotalWithdrawn
        /// 5. Ghi log WalletTransaction
        /// 6. Send notification cho shop
        /// 7. Background job sẽ xử lý chuyển khoản
        /// </summary>
        Task<WithdrawalRequest> ApproveWithdrawalAsync(string withdrawalId, string adminId, string? note = null);

        /// <summary>
        /// Admin reject withdrawal request
        /// </summary>
        Task<WithdrawalRequest> RejectWithdrawalAsync(string withdrawalId, string adminId, string reason);

        /// <summary>
        /// Background job xử lý chuyển khoản
        /// 
        /// FLOW:
        /// 1. Lấy approved withdrawals
        /// 2. Call bank transfer API (stub)
        /// 3. Nếu success → Status = Completed
        /// 4. Nếu failed → Status = Failed, HOÀN TIỀN vào AvailableBalance
        /// 5. Send notification
        /// </summary>
        Task ProcessApprovedWithdrawalsAsync();

        /// <summary>
        /// Lấy withdrawal request by ID
        /// </summary>
        Task<WithdrawalRequest?> GetWithdrawalRequestAsync(string withdrawalId);

        /// <summary>
        /// Lấy withdrawal requests theo shop
        /// </summary>
        Task<IEnumerable<WithdrawalRequest>> GetWithdrawalRequestsByShopAsync(int shopId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Lấy pending withdrawal requests
        /// </summary>
        Task<IEnumerable<WithdrawalRequest>> GetPendingWithdrawalRequestsAsync();

        /// <summary>
        /// Cancel withdrawal (chỉ khi Status = Pending)
        /// </summary>
        Task<WithdrawalRequest> CancelWithdrawalRequestAsync(string withdrawalId, string userId);
    }

    /// <summary>
    /// DTO để tạo withdrawal request
    /// </summary>
    public class CreateWithdrawalRequestDto
    {
        public int ShopId { get; set; }
        public decimal Amount { get; set; }
        public string BankName { get; set; } = null!;
        public string BankAccountNumber { get; set; } = null!;
        public string BankAccountName { get; set; } = null!;
        public string? BankBranch { get; set; }
        public string? Note { get; set; }
    }
}