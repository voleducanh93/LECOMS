using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Cart
{
    public class CartDTO
    {
        public string UserId { get; set; } = null!;
        public List<CartItemDTO> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
    }
}
