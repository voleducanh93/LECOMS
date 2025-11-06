using LECOMS.Service.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace LECOMS.Service.Jobs
{
    public class RecombeeSyncJob : IJob
    {
        private readonly RecombeeService _recombeeService;
        private readonly ILogger<RecombeeSyncJob> _logger;

        public RecombeeSyncJob(RecombeeService recombeeService, ILogger<RecombeeSyncJob> logger)
        {
            _recombeeService = recombeeService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("🔁 Bắt đầu đồng bộ sản phẩm Recommbee: {time}", DateTime.Now);
                int count = await _recombeeService.SyncProductsAsync();
                _logger.LogInformation("✅ Đồng bộ {count} sản phẩm sang Recommbee thành công!", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi đồng bộ dữ liệu Recommbee.");
            }
        }
    }
}
