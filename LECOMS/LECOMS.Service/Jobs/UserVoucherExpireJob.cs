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
    /// Job tự động đánh dấu UserVoucher bị expired.
    /// </summary>
    public class UserVoucherExpireJob : IJob
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<UserVoucherExpireJob> _logger;

        public UserVoucherExpireJob(IUnitOfWork uow, ILogger<UserVoucherExpireJob> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Tìm tất cả UserVoucher chưa dùng nhưng voucher đã hết hạn
                var userVouchers = await _uow.UserVouchers.GetAllAsync(
                    uv =>
                        !uv.IsUsed &&
                        uv.Voucher.EndDate.HasValue &&
                        uv.Voucher.EndDate.Value < now
                    , includeProperties: "Voucher"
                );

                if (!userVouchers.Any())
                {
                    _logger.LogInformation("UserVoucherExpireJob: No expired UserVoucher found.");
                    return;
                }

                foreach (var uv in userVouchers)
                {
                    uv.IsUsed = true;
                    uv.UsedAt = uv.Voucher.EndDate;
                    uv.OrderId = "EXPIRED";

                    await _uow.UserVouchers.UpdateAsync(uv);
                }

                await _uow.CompleteAsync();

                _logger.LogInformation(
                    "UserVoucherExpireJob: Marked {Count} UserVouchers expired at {Time}.",
                    userVouchers.Count(),
                    DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserVoucherExpireJob error");
            }
        }
    }
}
