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

        // =============================================================
        // 1) Get ALL achievements (completed + not completed)
        // =============================================================
        public async Task<IEnumerable<AchievementDTO>> GetAllAsync(string userId)
        {
            var defs = await _uow.AchievementDefinitions.Query()
                .Where(x => x.Active)
                .ToListAsync();

            var progressList = await _uow.UserAchievementProgresses.Query()
                .Where(x => x.UserId == userId)
                .ToListAsync();

            // convert progress to dictionary
            var map = progressList.ToDictionary(x => x.AchievementDefinitionId);

            var result = new List<AchievementDTO>();

            foreach (var def in defs)
            {
                map.TryGetValue(def.Id, out var progress);

                result.Add(new AchievementDTO
                {
                    Id = def.Id,
                    Code = def.Code,
                    Category = def.Category,
                    // ⭐ Fallback Image
                    ImageUrl = string.IsNullOrWhiteSpace(def.ImageUrl)
        ? "https://t3.ftcdn.net/jpg/02/14/16/04/360_F_214160413_Q0QybdSv1kg7Z4UyYKGKE1fNgvZt6z2M.jpg"
        : def.ImageUrl,
                    Title = def.Title,
                    Description = def.Description,

                    CurrentCount = progress?.CurrentValue ?? 0,
                    TargetCount = def.TargetValue,

                    XPReward = def.RewardXP,
                    CoinReward = def.RewardPoints,

                    IsCompleted = progress?.IsCompleted ?? false,
                    IsRewardClaimed = progress?.IsRewardClaimed ?? false,
                    CompletedAt = progress?.CompletedAt
                });
            }

            return result;
        }

        // =============================================================
        // 2) Get RECENT achievements (top 5 completed)
        // =============================================================
        public async Task<IEnumerable<RecentAchievementDTO>> GetRecentAsync(string userId)
        {
            var list = await _uow.UserAchievementProgresses.Query()
                .Include(x => x.Achievement)
                .Where(x => x.UserId == userId && x.IsCompleted)
                .OrderByDescending(x => x.CompletedAt)
                .Take(5)
                .ToListAsync();

            return list.Select(x => new RecentAchievementDTO
            {
                Id = x.AchievementDefinitionId,
                Title = x.Achievement.Title,
                Category = x.Achievement.Category,
                ImageUrl = string.IsNullOrWhiteSpace(x.Achievement.ImageUrl)
    ? "https://t3.ftcdn.net/jpg/02/14/16/04/360_F_214160413_Q0QybdSv1kg7Z4UyYKGKE1fNgvZt6z2M.jpg"
    : x.Achievement.ImageUrl,
                CompletedAt = x.CompletedAt ?? DateTime.UtcNow
            });
        }

        // =============================================================
        // 3) Get FULL HISTORY (all completed achievements)
        // =============================================================
        public async Task<IEnumerable<AchievementHistoryDTO>> GetHistoryAsync(string userId)
        {
            var list = await _uow.UserAchievementProgresses.Query()
                .Include(x => x.Achievement)
                .Where(x => x.UserId == userId && x.IsCompleted)
                .OrderByDescending(x => x.CompletedAt)
                .ToListAsync();

            return list.Select(x => new AchievementHistoryDTO
            {
                Id = x.AchievementDefinitionId,
                Title = x.Achievement.Title,
                Category = x.Achievement.Category,
                ImageUrl = string.IsNullOrWhiteSpace(x.Achievement.ImageUrl)
    ? "https://t3.ftcdn.net/jpg/02/14/16/04/360_F_214160413_Q0QybdSv1kg7Z4UyYKGKE1fNgvZt6z2M.jpg"
    : x.Achievement.ImageUrl,
                CompletedAt = x.CompletedAt ?? DateTime.UtcNow
            });
        }

        // =============================================================
        // 4) Increase progress of an achievement (called by logic events)
        // Example: complete lesson → Increase("ACHV_FIRST_LESSON")
        // =============================================================
        public async Task IncreaseProgressAsync(string userId, string achievementCode, int amount = 1)
        {
            var def = await _uow.AchievementDefinitions.GetAsync(x => x.Code == achievementCode);

            if (def == null)
                return; // achievement not exist -> ignore

            var progress = await _uow.UserAchievementProgresses.GetAsync(
                x => x.UserId == userId && x.AchievementDefinitionId == def.Id);

            // create progress if not exist
            if (progress == null)
            {
                progress = new UserAchievementProgress
                {
                    UserId = userId,
                    AchievementDefinitionId = def.Id,
                    CurrentValue = 0,
                };

                await _uow.UserAchievementProgresses.AddAsync(progress);
            }

            if (progress.IsCompleted)
                return; // already completed

            progress.CurrentValue += amount;

            // check complete
            if (progress.CurrentValue >= def.TargetValue)
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.UtcNow;
            }

            progress.UpdatedAt = DateTime.UtcNow;

            await _uow.CompleteAsync();
        }

        // =============================================================
        // 5) Claim reward
        // =============================================================
        public async Task<bool> ClaimRewardAsync(string userId, int achievementId)
        {
            var def = await _uow.AchievementDefinitions.GetAsync(x => x.Id == achievementId);
            if (def == null)
                return false;

            var progress = await _uow.UserAchievementProgresses.GetAsync(
                x => x.UserId == userId && x.AchievementDefinitionId == achievementId);

            if (progress == null || !progress.IsCompleted || progress.IsRewardClaimed)
                return false;

            // mark claimed
            progress.IsRewardClaimed = true;
            progress.UpdatedAt = DateTime.UtcNow;

            // ADD REWARD to PointWallet
            var wallet = await _uow.PointWallets.GetByUserIdAsync(userId);
            if (wallet == null)
            {
                wallet = new PointWallet
                {
                    UserId = userId,
                    Balance = 0,
                    LifetimeEarned = 0,
                    LifetimeSpent = 0
                };
                await _uow.PointWallets.AddAsync(wallet);
            }

            wallet.Balance += def.RewardPoints;
            wallet.LifetimeEarned += def.RewardPoints;

            // Ledger
            await _uow.PointLedgers.AddAsync(new PointLedger
            {
                PointWalletId = wallet.Id,
                Amount = def.RewardPoints,
                Type = Data.Enum.PointLedgerType.Earn,
                Description = $"Claim achievement: {def.Title}",
                CreatedAt = DateTime.UtcNow
            });

            await _uow.CompleteAsync();
            return true;
        }
        // =====================================================================
// BADGE LEGACY IMPLEMENTATION (để không lỗi interface)
// =====================================================================

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
