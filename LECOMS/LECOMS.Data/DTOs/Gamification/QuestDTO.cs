namespace LECOMS.Data.DTOs.Gamification
{
    public class QuestDTO
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;

        public string Period { get; set; } = null!;  // Daily/Weekly/Monthly

        public int CurrentValue { get; set; }
        public int TargetValue { get; set; }

        public int RewardXP { get; set; }
        public int RewardPoints { get; set; }

        /// <summary>InProgress | Completed | Claimed</summary>
        public string Status { get; set; } = null!;
        public bool IsRewardClaimed { get; set; }   // ⭐ NEW
    }
}
