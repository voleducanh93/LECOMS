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
    [Index(nameof(UserId), nameof(IsDefault), IsUnique = true)]
    public class Address
    {
        [Key] public string Id { get; set; } = Guid.NewGuid().ToString(); // nếu muốn dùng string key

        [Required] public string UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        [Required, MaxLength(150)] public string FullName { get; set; } = null!;
        [Required, MaxLength(20)] public string Phone { get; set; } = null!;
        [Required, MaxLength(255)] public string Line1 { get; set; } = null!;
        [MaxLength(255)] public string? Line2 { get; set; }
        [Required, MaxLength(100)] public string City { get; set; } = null!;
        [MaxLength(100)] public string? State { get; set; }
        [MaxLength(20)] public string? ZipCode { get; set; }
        [Required, MaxLength(100)] public string Country { get; set; } = "VN";

        public bool IsDefault { get; set; }
    }
}
