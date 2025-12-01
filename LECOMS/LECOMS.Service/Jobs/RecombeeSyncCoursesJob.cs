using LECOMS.Service.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace LECOMS.Service.Jobs
{
    public class RecombeeSyncCoursesJob : IJob
    {
        private readonly RecombeeService _recombeeService;
        private readonly ILogger<RecombeeSyncCoursesJob> _logger;

        public RecombeeSyncCoursesJob(RecombeeService recombeeService, ILogger<RecombeeSyncCoursesJob> logger)
        {
            _recombeeService = recombeeService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("🔁 Bắt đầu đồng bộ khóa học Recommbee: {time}", DateTime.Now);
                int count = await _recombeeService.SyncCoursesAsync();
                _logger.LogInformation("✅ Đồng bộ {count} khóa học sang Recommbee thành công!", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi đồng bộ khóa học lên Recommbee.");
            }
        }
    }
}
