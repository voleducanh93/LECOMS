using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Cart
{
    public class CartItemDTO
    {
        /// <summary>
        /// Product.Id
        /// </summary>
        public string ProductId { get; set; } = null!;

        /// <summary>
        /// Product.Name
        /// </summary>
        public string ProductName { get; set; } = null!;

        /// <summary>
        /// Product.Slug - dùng cho SEO URL
        /// </summary>
        public string ProductSlug { get; set; } = null!;

        /// <summary>
        /// Product.Price
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// CartItem.Quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Tính tự động: UnitPrice * Quantity
        /// </summary>
        public decimal LineTotal => UnitPrice * Quantity;

        /// <summary>
        /// ProductImage.Url (IsPrimary = true)
        /// </summary>
        public string? ProductImage { get; set; }
    }
}
