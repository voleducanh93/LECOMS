using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class PointWallet
    {
        [Key] public string Id { get; set; }

        [Required] public string UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        public int Balance { get; set; }
        public ICollection<PointLedger> Ledgers { get; set; } = new List<PointLedger>();
    }

}
