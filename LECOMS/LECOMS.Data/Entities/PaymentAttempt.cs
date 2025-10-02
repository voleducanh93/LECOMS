using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class PaymentAttempt
    {
        [Key] public string Id { get; set; }

        [Required] public string PaymentId { get; set; }
        [ForeignKey(nameof(PaymentId))] public Payment Payment { get; set; } = null!;

        public PaymentStatus Result { get; set; }
        [MaxLength(100)] public string? GatewayTxnId { get; set; }
        [MaxLength(2000)] public string? RawResponse { get; set; }
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    }
}
