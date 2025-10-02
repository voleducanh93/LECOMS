using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    [Index(nameof(LeaderboardId), nameof(UserId), IsUnique = true)]
    public class LeaderboardEntry
    {
        [Key] public string Id { get; set; }

        [Required] public string LeaderboardId { get; set; }
        [ForeignKey(nameof(LeaderboardId))] public Leaderboard Leaderboard { get; set; } = null!;

        [Required] public string UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        public int Score { get; set; }
        public int Rank { get; set; }
    }
}
