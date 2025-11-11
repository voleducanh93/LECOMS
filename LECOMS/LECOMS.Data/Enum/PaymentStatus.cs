namespace LECOMS.Data.Enum
{
    /// <summary>
    /// Trạng thái thanh toán của đơn hàng
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>
        /// Chờ thanh toán - Order vừa tạo, chưa thanh toán
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Đã thanh toán thành công qua PayOS
        /// Tiền đã vào platform wallet
        /// </summary>
        Paid = 1,

        /// <summary>
        /// Thanh toán thất bại
        /// </summary>
        Failed = 2,

        /// <summary>
        /// Đã hoàn tiền một phần
        /// </summary>
        PartiallyRefunded = 3,

        /// <summary>
        /// Đã hoàn tiền toàn bộ
        /// </summary>
        Refunded = 4,

        /// <summary>
        /// Đã hủy thanh toán
        /// </summary>
        Cancelled = 5
    }
}