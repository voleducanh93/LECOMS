using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class Badge
    {
        [Key] public string Id { get; set; }
        [Required, MaxLength(120)] public string Name { get; set; } = null!;
        [MaxLength(500)] public string? Description { get; set; }
    }
}
