namespace LECOMS.Data.DTOs.Order
{
    public class OrderDetailDTO
    {
        public string? Id { get; set; }

        public string ProductId { get; set; } = null!;
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal LineTotal => Quantity * UnitPrice;

        public string? ProductCategory { get; set; }

        // Mới: FE dùng để ẩn/hiện nút đánh giá
        public bool HasFeedback { get; set; } = false;

        // Tùy chọn: trả FeedbackId nếu cần (link trực tiếp)
        public string? FeedbackId { get; set; }
    }
}
