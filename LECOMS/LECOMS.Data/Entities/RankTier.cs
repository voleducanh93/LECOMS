using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class RankTier
    {
        [Key] public int TierID { get; set; }
        [Required, MaxLength(100)] public string TierName { get; set; } = null!;
        public int MinPoints { get; set; }
        [MaxLength(500)] public string? Benefits { get; set; }
        public byte Active { get; set; } = 1;
    }
}
