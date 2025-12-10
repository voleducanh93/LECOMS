using System.ComponentModel.DataAnnotations;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Master list các Achievement (định nghĩa, giống QuestDefinition)
    /// </summary>
    public class AchievementDefinition
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Mã nội bộ, dùng cho logic: "ACHV_FIRST_LESSON"</summary>
        [Required, MaxLength(100)]
        public string Code { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Nhóm hiển thị: "social", "learning", "account",...</summary>
        [Required, MaxLength(50)]
        public string Category { get; set; } = null!;

        /// <summary>Ảnh icon achievement</summary>
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        /// <summary>Target value (1 lesson, 3 lessons, 1 friend,...)</summary>
        public int TargetValue { get; set; }

        /// <summary>XP reward khi claim</summary>
        public int RewardXP { get; set; }

        /// <summary>Coin/Point reward khi claim</summary>
        public int RewardPoints { get; set; }

        public bool Active { get; set; } = true;
    }
}
