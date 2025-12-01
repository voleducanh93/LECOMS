using LECOMS.Data.DTOs.Withdrawal;
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
        Task<WithdrawalRequest> CreateWithdrawalRequestAsync(CreateWithdrawalRequestDto dto);
        Task<IEnumerable<WithdrawalRequest>> GetWithdrawalRequestsByShopAsync(int shopId, int pageNumber, int pageSize);
        Task<IEnumerable<WithdrawalRequest>> GetPendingWithdrawalRequestsAsync();
        Task<WithdrawalRequest?> GetWithdrawalRequestAsync(string withdrawalId);

        Task<WithdrawalRequest> ApproveWithdrawalAsync(string withdrawalId, string adminId, string? note);
        Task<WithdrawalRequest> CompleteWithdrawalAsync(string withdrawalId, string adminId);
        Task<WithdrawalRequest> RejectWithdrawalAsync(string withdrawalId, string adminId, string reason);

        Task<WithdrawalRequest> CancelWithdrawalRequestAsync(string withdrawalId, string sellerUserId);
        Task<ShopWithdrawalDetailDTO?> GetByIdAsync(string id);
    }
}