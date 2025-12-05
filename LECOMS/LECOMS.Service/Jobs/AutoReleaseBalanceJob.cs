using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
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
    /// Job tự động Release PendingBalance sang AvailableBalance
    /// cho các đơn hàng đã giao xong + hết thời gian refund window.
    /// </summary>
    public class AutoReleaseBalanceJob : IJob
    {
        private readonly IUnitOfWork _uow;
        private readonly IShopWalletService _shopWalletService;
        private readonly ILogger<AutoReleaseBalanceJob> _logger;

        public AutoReleaseBalanceJob(
            IUnitOfWork uow,
            IShopWalletService shopWalletService,
            ILogger<AutoReleaseBalanceJob> logger)
        {
            _uow = uow;
            _shopWalletService = shopWalletService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("🔄 AutoReleaseBalanceJob started at {Time}", DateTime.UtcNow);

                var config = await _uow.PlatformConfigs.GetConfigAsync();
                int holdingDays = config.OrderHoldingDays;

                var cutoff = DateTime.UtcNow.AddDays(-holdingDays);

                // Lấy đơn cần release
                var orders = await _uow.Orders.GetAllAsync(
                    o =>
                        o.PaymentStatus == PaymentStatus.Paid &&
                        o.Status == OrderStatus.Completed &&
                        o.CompletedAt.HasValue &&
                        o.CompletedAt.Value <= cutoff &&
                        o.BalanceReleased == false,
                    includeProperties: "User,Shop"
                );

                if (!orders.Any())
                {
                    _logger.LogInformation("Không có order nào cần release.");
                    return;
                }

                _logger.LogInformation("Tìm thấy {Count} đơn cần release.", orders.Count());

                foreach (var order in orders)
                {

                    try
                    {
                        // ❗ BỔ SUNG: Nếu đơn hàng đang có yêu cầu hoàn tiền đang xử lý
                        // thì KHÔNG được release tiền cho shop
                        var pendingRefund = await _uow.RefundRequests.GetAsync(
                            r => r.OrderId == order.Id &&
                                 (r.Status == RefundStatus.PendingShop ||
                                  r.Status == RefundStatus.PendingAdmin ||
                                  r.Status == RefundStatus.ShopApproved)
                        );

                        if (pendingRefund != null)
                        {
                            _logger.LogInformation(
                                "⏸ Bỏ qua đơn {OrderCode} vì đang có yêu cầu hoàn tiền trạng thái {Status}.",
                                order.OrderCode,
                                pendingRefund.Status);

                            continue;
                        }

                        var tx = await _uow.Transactions.GetByOrderIdAsync(order.Id);

                        if (tx == null)
                        {
                            _logger.LogWarning("Không tìm thấy giao dịch (Transaction) cho đơn hàng {OrderId}.", order.Id);
                            continue;
                        }

                        decimal shopAmount = tx.ShopAmount;

                        // Release
                        await _shopWalletService.ReleaseBalanceAsync(
                            order.ShopId,
                            shopAmount,
                            order.Id
                        );

                        order.BalanceReleased = true;
                        await _uow.Orders.UpdateAsync(order);
                        await _uow.CompleteAsync();

                        _logger.LogInformation(
                            "✔ Released {Amount} → Shop {ShopId} | Order {OrderCode}",
                            shopAmount, order.ShopId, order.OrderCode
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "❌ Lỗi release balance cho Order {OrderId}", order.Id);
                    }
                }

                _logger.LogInformation("🎉 AutoReleaseBalanceJob finished.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AutoReleaseBalanceJob failed.");
            }
        }
    }
}
