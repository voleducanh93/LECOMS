using LECOMS.Data.DTOs.Gamification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IAchievementService
    {
        Task<IEnumerable<RecentBadgeDTO>> GetRecentBadgesAsync(string userId);
        Task<IEnumerable<BadgeDTO>> GetAllBadgesAsync(string userId);
        Task<IEnumerable<BadgeHistoryDTO>> GetBadgeHistoryAsync(string userId);
        Task AwardBadgeAsync(string userId, string badgeId);
    }

}
