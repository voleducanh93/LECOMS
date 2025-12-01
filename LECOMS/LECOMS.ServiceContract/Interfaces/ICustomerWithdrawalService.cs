using LECOMS.Data.DTOs.Withdrawal;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    /// <summary>
    /// Service xử lý rút tiền từ CustomerWallet
    /// </summary>
    public interface ICustomerWithdrawalService
    {
        Task<CustomerWithdrawalRequest> CreateCustomerWithdrawalRequestAsync(CreateCustomerWithdrawalRequestDto dto);
        Task<IEnumerable<CustomerWithdrawalRequest>> GetCustomerWithdrawalRequestsByCustomerAsync(string customerId, int pageNumber, int pageSize);
        Task<IEnumerable<CustomerWithdrawalRequest>> GetPendingCustomerWithdrawalRequestsAsync();
        Task<CustomerWithdrawalRequest?> GetCustomerWithdrawalRequestAsync(string withdrawalId);

        Task<CustomerWithdrawalRequest> ApproveCustomerWithdrawalAsync(string withdrawalId, string adminId, string? note);
        Task<CustomerWithdrawalRequest> CompleteCustomerWithdrawalAsync(string withdrawalId, string adminId);
        Task<CustomerWithdrawalRequest> RejectCustomerWithdrawalAsync(string withdrawalId, string adminId, string reason);

        Task<CustomerWithdrawalRequest> CancelCustomerWithdrawalRequestAsync(string withdrawalId, string customerId);
        Task<CustomerWithdrawalDetailDTO?> GetByIdAsync(string id);
    }
}