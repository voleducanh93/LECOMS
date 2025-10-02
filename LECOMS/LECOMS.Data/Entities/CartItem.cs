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
    [Index(nameof(CartId), nameof(ProductId), IsUnique = true)]
    public class CartItem
    {
        [Key] public string Id { get; set; }

        [Required] public string CartId { get; set; }
        [ForeignKey(nameof(CartId))] public Cart Cart { get; set; } = null!;

        [Required] public string ProductId { get; set; }
        [ForeignKey(nameof(ProductId))] public Product Product { get; set; } = null!;

        [Range(1, int.MaxValue)] public int Quantity { get; set; }
    }
}
