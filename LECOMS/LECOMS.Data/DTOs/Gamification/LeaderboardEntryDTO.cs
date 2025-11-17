using System.Collections.Generic;

namespace LECOMS.Data.DTOs.Gamification
{
    public class LeaderboardEntryDTO
    {
        public int Rank { get; set; }
        public string UserId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string AvatarUrl { get; set; } = null!;
        public int Score { get; set; }
        public int Level { get; set; }
    }

    public class LeaderboardDTO
    {
        public string Period { get; set; } = null!; // Weekly / Monthly / AllTime
        public List<LeaderboardEntryDTO> Entries { get; set; } = new();
        public LeaderboardEntryDTO? CurrentUser { get; set; }
    }
}
