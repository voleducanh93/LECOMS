using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface ICustomerWalletService
    {
        Task<CustomerWallet> GetOrCreateWalletAsync(string customerId);

        Task<CustomerWallet?> GetWalletWithTransactionsAsync(
            string customerId, int pageNumber = 1, int pageSize = 20);

        Task<CustomerWallet> AddBalanceAsync(
            string customerId, decimal amount, string refundId, string description);

        Task<CustomerWallet> DeductBalanceAsync(
            string customerId,
            decimal amount,
            WalletTransactionType type,
            string referenceId,
            string description);

        Task<bool> HasSufficientBalanceAsync(string customerId, decimal amount);

        Task<decimal> GetBalanceAsync(string customerId);
    }
}
