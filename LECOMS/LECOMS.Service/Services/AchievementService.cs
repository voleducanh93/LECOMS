using LECOMS.Data.DTOs.Gamification;
using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LECOMS.Service.Services
{
    public class AchievementService : IAchievementService
    {
        private readonly IUnitOfWork _uow;

        public AchievementService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // 1. Recent 5 badges
        public async Task<IEnumerable<RecentBadgeDTO>> GetRecentBadgesAsync(string userId)
        {
            var list = await _uow.UserBadges.Query()
                .Include(x => x.Badge)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.AchievedAt)
                .Take(5)
                .ToListAsync();

            return list.Select(x => new RecentBadgeDTO
            {
                Id = x.BadgeId,
                Name = x.Badge.Name,
                Description = x.Badge.Description,
                AchievedAt = x.AchievedAt
            });
        }

        // 2. All badges (earned + not earned)
        public async Task<IEnumerable<BadgeDTO>> GetAllBadgesAsync(string userId)
        {
            var all = await _uow.Badges.GetAllAsync();
            var owned = await _uow.UserBadges.GetAllAsync(x => x.UserId == userId);

            var map = owned.ToDictionary(x => x.BadgeId, x => x.AchievedAt);

            return all.Select(b => new BadgeDTO
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                IsAchieved = map.ContainsKey(b.Id),
                AchievedAt = map.ContainsKey(b.Id) ? map[b.Id] : null
            });
        }

        // 3. Full history
        public async Task<IEnumerable<BadgeHistoryDTO>> GetBadgeHistoryAsync(string userId)
        {
            var list = await _uow.UserBadges.Query()
                .Include(x => x.Badge)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.AchievedAt)
                .ToListAsync();

            return list.Select(x => new BadgeHistoryDTO
            {
                BadgeId = x.BadgeId,
                BadgeName = x.Badge.Name,
                AchievedAt = x.AchievedAt
            });
        }

        // 4. Award badge if not exists
        public async Task AwardBadgeAsync(string userId, string badgeId)
        {
            var exists = await _uow.UserBadges.GetAsync(
                x => x.UserId == userId && x.BadgeId == badgeId);

            if (exists != null)
                return;

            var ub = new UserBadge
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                BadgeId = badgeId,
                AchievedAt = DateTime.UtcNow
            };

            await _uow.UserBadges.AddAsync(ub);
            await _uow.CompleteAsync();
        }
    }
}
