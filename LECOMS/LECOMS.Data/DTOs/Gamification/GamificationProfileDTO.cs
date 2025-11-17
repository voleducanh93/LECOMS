using System.Collections.Generic;

namespace LECOMS.Data.DTOs.Gamification
{
    public class GamificationProfileDTO
    {
        public int Level { get; set; }
        public int CurrentXP { get; set; }
        public int XpToNextLevel { get; set; }

        public int Coins { get; set; }
        public int DailyStreak { get; set; }

        public List<QuestDTO> DailyQuests { get; set; } = new();
        public List<QuestDTO> WeeklyQuests { get; set; } = new();
        public List<QuestDTO> MonthlyQuests { get; set; } = new();
    }
}
