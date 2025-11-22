using LECOMS.RepositoryContract.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Service.Jobs
{
    /// <summary>
    /// Job tự động disable voucher hết hạn mỗi 1 giờ.
    /// </summary>
    public class VoucherExpireJob : IJob
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<VoucherExpireJob> _logger;

        public VoucherExpireJob(IUnitOfWork uow, ILogger<VoucherExpireJob> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Voucher đã hết hạn nhưng vẫn IsActive = true
                var expired = await _uow.Vouchers.GetAllAsync(
                    v => v.IsActive &&
                         v.EndDate.HasValue &&
                         v.EndDate.Value < now
                );

                if (!expired.Any())
                {
                    _logger.LogInformation("VoucherExpireJob: No expired voucher found.");
                    return;
                }

                foreach (var voucher in expired)
                {
                    voucher.IsActive = false;
                    await _uow.Vouchers.UpdateAsync(voucher);
                }

                await _uow.CompleteAsync();

                _logger.LogInformation(
                    "VoucherExpireJob: Disabled {Count} expired vouchers at {Time}.",
                    expired.Count(),
                    DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VoucherExpireJob error");
            }
        }
    }
}
