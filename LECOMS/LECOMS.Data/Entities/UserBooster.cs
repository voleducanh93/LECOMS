using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    [Index(nameof(UserId), nameof(BoosterId), nameof(AcquiredAt))]
    public class UserBooster
    {
        [Key] public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required] public string UserId { get; set; } = null!;
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        [Required] public int BoosterId { get; set; }
        [ForeignKey(nameof(BoosterId))] public Booster Booster { get; set; } = null!;

        public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
        public DateTime? ActivatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsConsumed { get; set; }
    }
}
