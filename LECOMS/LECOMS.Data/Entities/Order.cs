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
    [Index(nameof(UserId), nameof(CreatedAt))]
    public class Order
    {
        [Key] public string Id { get; set; }

        [Required] public string UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        // Snapshot địa chỉ & giá
        [Required, MaxLength(255)] public string ShipToName { get; set; } = null!;
        [Required, MaxLength(20)] public string ShipToPhone { get; set; } = null!;
        [Required, MaxLength(500)] public string ShipToAddress { get; set; } = null!;

        [Precision(18, 2)] public decimal Subtotal { get; set; }
        [Precision(18, 2)] public decimal ShippingFee { get; set; }
        [Precision(18, 2)] public decimal Discount { get; set; }
        [Precision(18, 2)] public decimal Total { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
