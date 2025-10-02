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
    [Index(nameof(UserId), nameof(ProductId), IsUnique = true)]
    public class Review
    {
        [Key] public string Id { get; set; }

        [Required] public string UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        [Required] public string ProductId { get; set; }
        [ForeignKey(nameof(ProductId))] public Product Product { get; set; } = null!;

        [Range(1, 5)] public int Rating { get; set; }
        [MaxLength(1000)] public string? Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
