using LECOMS.Data.DTOs.Voucher;
using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IVoucherService
    {
        Task<VoucherApplyResultDTO> ValidateAndPreviewAsync(
            string userId,
            string voucherCode,
            IEnumerable<Order> orders);

        Task MarkVoucherUsedAsync(
            string userId,
            string voucherCode,
            IEnumerable<Order> orders,
            string paymentReference);

        Task<IEnumerable<UserVoucherDTO>> GetMyVouchersAsync(string userId);
    }
}
