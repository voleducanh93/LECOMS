using LECOMS.Data.DTOs.Gamification;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IAchievementService
    {
        // ============================================================
        // ACHIEVEMENT (New System)
        // ============================================================

        /// <summary>Lấy toàn bộ achievement (đã + chưa hoàn thành).</summary>
        Task<IEnumerable<AchievementDTO>> GetAllAsync(string userId);

        /// <summary>Lấy 5 achievement gần nhất.</summary>
        Task<IEnumerable<RecentAchievementDTO>> GetRecentAsync(string userId);

        /// <summary>Lấy toàn bộ lịch sử achievement đã hoàn thành.</summary>
        Task<IEnumerable<AchievementHistoryDTO>> GetHistoryAsync(string userId);

        /// <summary>Tăng tiến độ achievement. Dùng trong event triggers.</summary>
        Task IncreaseProgressAsync(string userId, string achievementCode, int amount = 1);

        /// <summary>Nhận thưởng achievement sau khi hoàn thành.</summary>
        Task<bool> ClaimRewardAsync(string userId, int achievementId);


        // ============================================================
        // BADGES (Legacy - giữ lại để không breaking API cũ)
        // ============================================================

        Task<IEnumerable<RecentBadgeDTO>> GetRecentBadgesAsync(string userId);
        Task<IEnumerable<BadgeDTO>> GetAllBadgesAsync(string userId);
        Task<IEnumerable<BadgeHistoryDTO>> GetBadgeHistoryAsync(string userId);

        /// <summary>Award badge ngay lập tức (legacy).</summary>
        Task AwardBadgeAsync(string userId, string badgeId);
    }
}
