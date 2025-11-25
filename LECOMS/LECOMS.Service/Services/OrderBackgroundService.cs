using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    /// <summary>
    /// Background service để:
    /// 1. Release balance sau holding period
    /// 2. Process approved withdrawals
    /// </summary>
    public class OrderBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderBackgroundService> _logger;

        public OrderBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<OrderBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Dịch vụ nền đặt hàng đã bắt đầu");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        // 1. Release balances
                        await ReleaseOrderBalancesAsync(scope);

                        // 2. Process withdrawals
                        await ProcessWithdrawalsAsync(scope);
                    }

                    // Run every 1 hour
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi trong OrderBackgroundService");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Dịch vụ nền đặt hàng đã dừng");
        }

        /// <summary>
        /// Release balance cho các orders đã qua holding period
        /// </summary>
        private async Task ReleaseOrderBalancesAsync(IServiceScope scope)
        {
            try
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var shopWalletService = scope.ServiceProvider.GetRequiredService<IShopWalletService>();

                // Lấy platform config
                var config = await unitOfWork.PlatformConfigs.GetConfigAsync();

                // Lấy orders đã completed + đã qua holding period + chưa release
                var cutoffDate = DateTime.UtcNow.AddDays(-config.OrderHoldingDays);

                var ordersToRelease = await unitOfWork.Orders.GetAllAsync(
                    o => o.PaymentStatus == PaymentStatus.Paid
                        && o.Status == OrderStatus.Completed
                        && o.CompletedAt.HasValue
                        && o.CompletedAt.Value <= cutoffDate
                        && !o.BalanceReleased,
                    includeProperties: "Shop");

                _logger.LogInformation("Found {Count} orders to release balance", ordersToRelease.Count());

                foreach (var order in ordersToRelease)
                {
                    try
                    {
                        // Lấy transaction để biết shop amount
                        var transaction = await unitOfWork.Transactions.GetByOrderIdAsync(order.Id);
                        if (transaction == null)
                        {
                            _logger.LogWarning("Transaction không tìm thấy for Order {OrderId}", order.Id);
                            continue;
                        }

                        // Release balance
                        await shopWalletService.ReleaseBalanceAsync(
                            order.ShopId,
                            transaction.ShopAmount,
                            order.Id);

                        // Update order
                        order.BalanceReleased = true;
                        await unitOfWork.Orders.UpdateAsync(order);
                        await unitOfWork.CompleteAsync();

                        _logger.LogInformation(
                            "Released balance for Order {OrderId}, Shop {ShopId}, Amount: {Amount}",
                            order.Id, order.ShopId, transaction.ShopAmount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error releasing balance for Order {OrderId}", order.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReleaseOrderBalancesAsync");
            }
        }

        /// <summary>
        /// Process approved Yêu cầu rút tiền
        /// </summary>
        private async Task ProcessWithdrawalsAsync(IServiceScope scope)
        {
            try
            {
                var withdrawalService = scope.ServiceProvider.GetRequiredService<IWithdrawalService>();

                await withdrawalService.ProcessApprovedWithdrawalsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessWithdrawalsAsync");
            }
        }
    }
}