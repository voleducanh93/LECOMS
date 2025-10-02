using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class ShipmentItem
    {
        [Key] public string Id { get; set; }

        [Required] public string ShipmentId { get; set; }
        [ForeignKey(nameof(ShipmentId))] public Shipment Shipment { get; set; } = null!;

        [Required] public string OrderDetailId { get; set; }
        [ForeignKey(nameof(OrderDetailId))] public OrderDetail OrderDetail { get; set; } = null!;

        [Range(1, int.MaxValue)] public int Quantity { get; set; }
    }
}
