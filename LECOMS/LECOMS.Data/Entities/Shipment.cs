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
    public class Shipment
    {
        [Key] public string Id { get; set; }

        [Required] public string OrderId { get; set; }
        [ForeignKey(nameof(OrderId))] public Order Order { get; set; } = null!;

        [MaxLength(100)] public string? Carrier { get; set; }
        [MaxLength(100)] public string? TrackingNumber { get; set; }
        public ShipmentStatus Status { get; set; } = ShipmentStatus.Ready;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ShipmentItem> Items { get; set; } = new List<ShipmentItem>();
    }
}
