using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Tiến độ Achievement theo user (giống UserQuestProgress)
    /// </summary>
    [Index(nameof(UserId), nameof(AchievementDefinitionId), IsUnique = true)]
    public class UserAchievementProgress
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Required]
        public int AchievementDefinitionId { get; set; }

        [ForeignKey(nameof(AchievementDefinitionId))]
        public AchievementDefinition Achievement { get; set; } = null!;

        /// <summary>Giá trị hiện tại (số lesson đã học, số bạn đã mời,...)</summary>
        public int CurrentValue { get; set; }

        public bool IsCompleted { get; set; }

        /// <summary>Đã nhận thưởng chưa</summary>
        public bool IsRewardClaimed { get; set; }

        /// <summary>Thời điểm hoàn thành (đủ target)</summary>
        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
