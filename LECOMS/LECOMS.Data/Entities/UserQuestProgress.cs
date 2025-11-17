using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    [Index(nameof(UserId), nameof(QuestDefinitionId), nameof(PeriodStart), IsUnique = true)]
    public class UserQuestProgress
    {
        [Key] public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required] public string UserId { get; set; } = null!;
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        [Required] public int QuestDefinitionId { get; set; }
        [ForeignKey(nameof(QuestDefinitionId))] public QuestDefinition Quest { get; set; } = null!;

        public int CurrentValue { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsClaimed { get; set; }

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
