using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class EarnRule
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(120)] public string Action { get; set; } = null!; // e.g., "CompleteLesson"
        public int Points { get; set; }
        public bool Active { get; set; } = true;
    }

}
