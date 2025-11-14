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

        /// <summary>
        /// Items đã được group theo Shop
        /// </summary>
        public List<ShopGroupedItems> Items { get; set; } = new List<ShopGroupedItems>();

        /// <summary>
        /// Tổng tiền toàn bộ giỏ hàng
        /// </summary>
        public decimal Subtotal { get; set; }
    }
}
