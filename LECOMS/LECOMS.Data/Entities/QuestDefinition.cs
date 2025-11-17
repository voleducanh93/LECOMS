using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class QuestDefinition
    {
        [Key] public int Id { get; set; }

        [Required, MaxLength(100)] public string Code { get; set; } = null!; // COMPLETE_3_LESSONS
        [Required, MaxLength(200)] public string Title { get; set; } = null!;
        [MaxLength(500)] public string? Description { get; set; }

        public QuestPeriod Period { get; set; }

        /// <summary>Target value (3 lessons, 2 videos,...)</summary>
        public int TargetValue { get; set; }

        public int RewardXP { get; set; }
        public int RewardPoints { get; set; }

        public bool Active { get; set; } = true;
    }
}
