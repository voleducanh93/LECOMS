using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class RedeemRule
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(120)] public string Reward { get; set; } = null!; // e.g., "Voucher10K"
        public int CostPoints { get; set; }
        public bool Active { get; set; } = true;
    }

}
