namespace LECOMS.Data.Enum
{
    /// <summary>
    /// Trạng thái refund request
    /// Simple flow: Shop decides, no admin needed
    /// </summary>
    public enum RefundStatus
    {
        /// <summary>
        /// Chờ shop xem xét (3 days)
        /// </summary>
        PendingShopApproval = 0,

        /// <summary>
        /// Đang xử lý refund (chuyển tiền)
        /// </summary>
        Processing = 1,

        /// <summary>
        /// Hoàn tất - tiền đã vào CustomerWallet
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Shop từ chối - Customer có thể review
        /// </summary>
        ShopRejected = 3,

        /// <summary>
        /// Thất bại (technical error)
        /// </summary>
        Failed = 4,

        /// <summary>
        /// Customer tự hủy
        /// </summary>
        Cancelled = 5
    }
}