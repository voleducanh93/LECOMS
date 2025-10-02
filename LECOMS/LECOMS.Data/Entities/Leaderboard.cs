using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    [Index(nameof(Code), IsUnique = true)]
    public class Leaderboard
    {
        [Key] public string Id { get; set; }

        [Required, MaxLength(50)] public string Code { get; set; } = "GLOBAL";
        [MaxLength(50)] public string Period { get; set; } = "Monthly"; // Daily/Weekly/Monthly/Season
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        public ICollection<LeaderboardEntry> Entries { get; set; } = new List<LeaderboardEntry>();
    }
}
