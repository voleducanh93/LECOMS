using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class Booster
    {
        [Key] public int Id { get; set; }

        [Required, MaxLength(100)] public string Code { get; set; } = null!;  // DOUBLE_XP_24H
        [Required, MaxLength(200)] public string Name { get; set; } = null!;
        [MaxLength(500)] public string? Description { get; set; }

        public BoosterEffectType EffectType { get; set; }
        public int CostPoints { get; set; }

        /// <summary>Thời gian hiệu lực sau khi kích hoạt (null = one-time)</summary>
        public TimeSpan? Duration { get; set; }

        public bool Active { get; set; } = true;
    }
}
