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
    public class WalletAccount
    {
        [Key] public string Id { get; set; }

        [Required] public string UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        [Precision(18, 2)] public decimal Balance { get; set; }
        [MaxLength(3)] public string Currency { get; set; } = "VND";

        public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    }
}
