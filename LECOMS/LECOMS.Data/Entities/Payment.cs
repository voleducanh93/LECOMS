using LECOMS.Data.Enum;
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
    public class Payment
    {
        [Key] public string Id { get; set; }

        [Required] public string OrderId { get; set; }
        [ForeignKey(nameof(OrderId))] public Order Order { get; set; } = null!;

        [Precision(18, 2)] public decimal Amount { get; set; }
        [MaxLength(50)] public string Provider { get; set; } = "COD/PayOS/VnPay";
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PaymentAttempt> Attempts { get; set; } = new List<PaymentAttempt>();
    }
}
