using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Cart
{
    /// <summary>
    /// Items trong cart được group theo Shop
    /// </summary>
    public class ShopGroupedItems
    {
        /// <summary>
        /// ID của shop (int - khớp với Shop.Id)
        /// </summary>
        public int ShopId { get; set; }

        /// <summary>
        /// Tên shop (khớp với Shop.Name)
        /// </summary>
        public string ShopName { get; set; } = null!;

        /// <summary>
        /// Avatar shop (khớp với Shop.ShopAvatar)
        /// </summary>
        public string? ShopAvatar { get; set; }

        /// <summary>
        /// Danh sách sản phẩm của shop này trong cart
        /// </summary>
        public List<CartItemDTO> Items { get; set; } = new List<CartItemDTO>();

        /// <summary>
        /// Tổng tiền của shop này
        /// </summary>
        public decimal Subtotal => Items.Sum(i => i.LineTotal);
    }
}
