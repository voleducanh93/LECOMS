namespace LECOMS.Data.Enum
{
    /// <summary>
    /// Trạng thái của transaction thanh toán qua PayOS
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>
        /// Đang chờ customer thanh toán qua PayOS
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Thanh toán hoàn tất
        /// Tiền đã vào platform, đã tính commission
        /// </summary>
        Completed = 1,

        /// <summary>
        /// Thanh toán thất bại
        /// </summary>
        Failed = 2,

        /// <summary>
        /// Đã hoàn tiền toàn bộ
        /// </summary>
        Refunded = 3,

        /// <summary>
        /// Đã hoàn tiền một phần
        /// </summary>
        PartiallyRefunded = 4,
        /// <summary>
        /// Đã hủy thanh toán
        /// </summary>
        Cancelled = 5
    }
}