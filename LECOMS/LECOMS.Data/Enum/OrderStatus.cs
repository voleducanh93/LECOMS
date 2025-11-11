namespace LECOMS.Data.Enum
{
    /// <summary>
    /// Trạng thái của Order
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// Chờ thanh toán
        /// Order vừa tạo, đang chờ customer thanh toán qua PayOS
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Đã thanh toán
        /// PayOS webhook confirmed payment success
        /// Tiền đã vào ShopWallet.PendingBalance
        /// Shop có thể bắt đầu chuẩn bị hàng
        /// </summary>
        Paid = 1,

        /// <summary>
        /// Đang xử lý / chuẩn bị hàng
        /// Shop đang đóng gói, chuẩn bị giao hàng
        /// </summary>
        Processing = 2,

        /// <summary>
        /// Đang giao hàng
        /// Đơn hàng đã được giao cho đơn vị vận chuyển
        /// </summary>
        Shipping = 3,

        /// <summary>
        /// Đã hoàn thành
        /// Customer đã nhận hàng và xác nhận (ConfirmReceived)
        /// Sau OrderHoldingDays → Balance sẽ release cho shop
        /// </summary>
        Completed = 4,

        /// <summary>
        /// Đã hủy
        /// Order bị hủy bởi customer hoặc shop
        /// Nếu đã thanh toán → cần refund
        /// </summary>
        Cancelled = 5,

        /// <summary>
        /// Đã hoàn tiền
        /// Refund đã được xử lý thành công
        /// Tiền đã vào CustomerWallet hoặc ShopWallet (tùy trường hợp)
        /// </summary>
        Refunded = 6,

        /// <summary>
        /// Thanh toán thất bại
        /// PayOS payment failed
        /// Order sẽ tự động cancelled sau một khoảng thời gian
        /// </summary>
        PaymentFailed = 7
    }
}