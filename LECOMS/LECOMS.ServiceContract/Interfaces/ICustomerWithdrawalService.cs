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
        /// <summary>
        /// Customer tạo withdrawal request
        /// 
        /// FLOW:
        /// 1. Validate amount (min, max từ PlatformConfig)
        /// 2. Validate Balance >= amount
        /// 3. Validate bank info
        /// 4. Tạo CustomerWithdrawalRequest (Status = Pending)
        /// 5. Send notification cho admin
        /// </summary>
        Task<CustomerWithdrawalRequest> CreateCustomerWithdrawalRequestAsync(CreateCustomerWithdrawalRequestDto dto);

        /// <summary>
        /// Admin approve withdrawal request
        /// 
        /// FLOW:
        /// 1. Validate Balance vẫn đủ
        /// 2. TRỪ TIỀN khỏi Balance NGAY (lock)
        /// 3. Update Status = Approved
        /// 4. Update TotalWithdrawn
        /// 5. Ghi log CustomerWalletTransaction
        /// 6. Send notification cho customer
        /// 7. Background job sẽ xử lý chuyển khoản
        /// </summary>
        Task<CustomerWithdrawalRequest> ApproveCustomerWithdrawalAsync(string withdrawalId, string adminId, string? note = null);

        /// <summary>
        /// Admin reject withdrawal request
        /// </summary>
        Task<CustomerWithdrawalRequest> RejectCustomerWithdrawalAsync(string withdrawalId, string adminId, string reason);

        /// <summary>
        /// Background job xử lý chuyển khoản
        /// 
        /// FLOW:
        /// 1. Lấy approved withdrawals
        /// 2. Call bank transfer API (stub)
        /// 3. Nếu success → Status = Completed
        /// 4. Nếu failed → Status = Failed, HOÀN TIỀN vào Balance
        /// 5. Send notification
        /// </summary>
        Task ProcessApprovedCustomerWithdrawalsAsync();

        /// <summary>
        /// Lấy withdrawal request by ID
        /// </summary>
        Task<CustomerWithdrawalRequest?> GetCustomerWithdrawalRequestAsync(string withdrawalId);

        /// <summary>
        /// Lấy withdrawal requests theo customer
        /// </summary>
        Task<IEnumerable<CustomerWithdrawalRequest>> GetCustomerWithdrawalRequestsByCustomerAsync(string customerId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Lấy pending withdrawal requests
        /// </summary>
        Task<IEnumerable<CustomerWithdrawalRequest>> GetPendingCustomerWithdrawalRequestsAsync();

        /// <summary>
        /// Cancel withdrawal (chỉ khi Status = Pending)
        /// </summary>
        Task<CustomerWithdrawalRequest> CancelCustomerWithdrawalRequestAsync(string withdrawalId, string customerId);
    }

    /// <summary>
    /// DTO để tạo customer withdrawal request
    /// </summary>
    public class CreateCustomerWithdrawalRequestDto
    {
        public string CustomerId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string BankName { get; set; } = null!;
        public string BankAccountNumber { get; set; } = null!;
        public string BankAccountName { get; set; } = null!;
        public string? BankBranch { get; set; }
        public string? Note { get; set; }
    }
}