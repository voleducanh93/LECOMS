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
    [Index(nameof(OrderId), nameof(ProductId), IsUnique = true)]
    public class OrderDetail
    {
        [Key] public string Id { get; set; }

        [Required] public string OrderId { get; set; }
        [ForeignKey(nameof(OrderId))] public Order Order { get; set; } = null!;

        [Required] public string ProductId { get; set; }
        [ForeignKey(nameof(ProductId))] public Product Product { get; set; } = null!;

        [Range(1, int.MaxValue)] public int Quantity { get; set; }
        [Precision(18, 2)] public decimal UnitPrice { get; set; } // snapshot
    }
}
