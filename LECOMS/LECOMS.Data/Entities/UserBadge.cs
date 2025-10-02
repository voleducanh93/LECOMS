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
    [Index(nameof(UserId), nameof(BadgeId), IsUnique = true)]
    public class UserBadge
    {
        [Key] public string Id { get; set; }

        [Required] public string UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        [Required] public string BadgeId { get; set; }
        [ForeignKey(nameof(BadgeId))] public Badge Badge { get; set; } = null!;

        public DateTime AchievedAt { get; set; } = DateTime.UtcNow;
    }

}
