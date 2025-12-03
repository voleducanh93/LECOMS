using LECOMS.Data.Enum;
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
    /// Job tự động đẩy các yêu cầu hoàn tiền từ PendingShop lên PendingAdmin
    /// nếu người bán không phản hồi trong thời gian được cấu hình.
    /// </summary>
    public class AutoEscalateRefundJob : IJob
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AutoEscalateRefundJob> _logger;

        public AutoEscalateRefundJob(
            IUnitOfWork uow,
            ILogger<AutoEscalateRefundJob> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("🔄 Bắt đầu AutoEscalateRefundJob lúc {Time}", DateTime.UtcNow);

                var config = await _uow.PlatformConfigs.GetConfigAsync();

                // Số giờ cho phép người bán phản hồi trước khi hệ thống tự động đẩy lên Admin
                // TODO: Đảm bảo đã thêm trường SellerRefundResponseHours (int) trong PlatformConfigs.
                int sellerResponseHours = config.SellerRefundResponseHours;
                if (sellerResponseHours <= 0)
                {
                    sellerResponseHours = 48; // fallback mặc định 48h nếu chưa cấu hình
                }

                var cutoff = DateTime.UtcNow.AddHours(-sellerResponseHours);

                // Lấy các yêu cầu hoàn tiền mà:
                // - Đang ở trạng thái PendingShop
                // - Thời gian yêu cầu <= cutoff (đã quá hạn phản hồi của người bán)
                var pendingShopRefunds = await _uow.RefundRequests.GetAllAsync(
                    r => r.Status == RefundStatus.PendingShop &&
                         r.RequestedAt <= cutoff,
                    includeProperties: "Order,Order.Shop,RequestedByUser"
                );

                if (!pendingShopRefunds.Any())
                {
                    _logger.LogInformation(
                        "✅ Không có yêu cầu hoàn tiền PendingShop nào quá hạn phản hồi cần chuyển lên Admin.");
                    return;
                }

                _logger.LogInformation(
                    "🔍 Tìm thấy {Count} yêu cầu hoàn tiền PendingShop đã quá hạn phản hồi của người bán.",
                    pendingShopRefunds.Count());

                int updatedCount = 0;

                foreach (var refund in pendingShopRefunds)
                {
                    try
                    {
                        refund.Status = RefundStatus.PendingAdmin;

                        // Thêm ghi chú để Admin / Customer hiểu lý do
                        const string note = "Hệ thống tự động chuyển yêu cầu hoàn tiền lên Quản trị viên do người bán không phản hồi đúng hạn.";

                        if (string.IsNullOrWhiteSpace(refund.ProcessNote))
                        {
                            refund.ProcessNote = note;
                        }
                        else if (!refund.ProcessNote.Contains(note))
                        {
                            refund.ProcessNote += " | " + note;
                        }

                        await _uow.RefundRequests.UpdateAsync(refund);
                        updatedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "❌ Lỗi khi tự động chuyển yêu cầu hoàn tiền {RefundId} lên PendingAdmin.",
                            refund.Id);
                    }
                }

                if (updatedCount > 0)
                {
                    await _uow.CompleteAsync();
                }

                _logger.LogInformation(
                    "🎉 AutoEscalateRefundJob hoàn thành. Đã chuyển {Count} yêu cầu hoàn tiền lên PendingAdmin.",
                    updatedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AutoEscalateRefundJob gặp lỗi tổng thể.");
            }
        }
    }
}
