using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class UserTierHistory
    {
        [Key] public string Id { get; set; }

        [Required] public string UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        [Required] public int TierID { get; set; }
        [ForeignKey(nameof(TierID))] public RankTier Tier { get; set; } = null!;

        public DateTime FromDate { get; set; } = DateTime.UtcNow;
        public DateTime? ToDate { get; set; }
    }
}
