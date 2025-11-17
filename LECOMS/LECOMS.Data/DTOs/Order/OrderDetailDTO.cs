namespace LECOMS.Data.DTOs.Order
{
    /// <summary>
    /// DTO cho OrderDetail
    /// </summary>
    public class OrderDetailDTO
    {
        /// <summary>
        /// ID của OrderDetail
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Product ID
        /// </summary>
        public string ProductId { get; set; } = null!;

        /// <summary>
        /// Tên sản phẩm
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Hình ảnh sản phẩm (primary image)
        /// </summary>
        public string? ProductImage { get; set; }

        /// <summary>
        /// Số lượng
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Giá tại thời điểm order (snapshot)
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Thành tiền (Quantity * UnitPrice)
        /// </summary>
        public decimal LineTotal => Quantity * UnitPrice;

        /// <summary>
        /// SKU của product (optional)
        /// </summary>
        //public string? ProductSku { get; set; }

        /// <summary>
        /// Category của product (optional)
        /// </summary>
        public string? ProductCategory { get; set; }
    }
}