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
}
