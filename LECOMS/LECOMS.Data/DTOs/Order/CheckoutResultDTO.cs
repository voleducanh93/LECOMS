using System.Collections.Generic;

namespace LECOMS.Data.DTOs.Order
{
    /// <summary>
    /// DTO trả về khi checkout thành công
    /// Theo mô hình SÀN THU HỘ
    /// </summary>
    public class CheckoutResultDTO
    {
        /// <summary>
        /// Danh sách orders được tạo
        /// Có thể nhiều orders nếu cart có items từ nhiều shops
        /// Example: Cart có 3 shops → 3 orders
        /// </summary>
        public List<OrderDTO> Orders { get; set; } = new List<OrderDTO>();

        /// <summary>
        /// ⭐ Payment URL DUY NHẤT cho toàn bộ đơn hàng
        /// 
        /// MÔ HÌNH SÀN THU HỘ:
        /// - Customer chỉ thanh toán 1 LẦN cho tất cả orders
        /// - Platform nhận tiền trước
        /// - Platform tự động chia tiền cho các shops
        /// 
        /// VD: Cart có 3 shops (100k + 200k + 150k = 450k)
        ///     → Customer thanh toán 1 lần: 450k
        ///     → 1 PaymentUrl duy nhất
        /// </summary>
        public string PaymentUrl { get; set; } = string.Empty;

        /// <summary>
        /// Tổng số tiền customer cần thanh toán
        /// = Tổng tất cả orders + shipping fee - discount
        /// </summary>
        public decimal TotalAmount { get; set; }
    }
}