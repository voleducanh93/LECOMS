namespace LECOMS.Data.DTOs.Gamification
{
    public class RecentBadgeDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime AchievedAt { get; set; }
    }

    public class BadgeDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsAchieved { get; set; }
        public DateTime? AchievedAt { get; set; }
    }

    public class BadgeHistoryDTO
    {
        public string BadgeId { get; set; } = null!;
        public string BadgeName { get; set; } = null!;
        public DateTime AchievedAt { get; set; }
    }
    public class AchievementDTO
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;

        public string Category { get; set; } = null!;
        public string? ImageUrl { get; set; }

        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public int CurrentCount { get; set; }
        public int TargetCount { get; set; }

        public int XPReward { get; set; }
        public int CoinReward { get; set; }

        public bool IsCompleted { get; set; }
        public bool IsRewardClaimed { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
    public class AchievementHistoryDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public DateTime CompletedAt { get; set; }
    }
    public class RecentAchievementDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public DateTime CompletedAt { get; set; }
    }

}
