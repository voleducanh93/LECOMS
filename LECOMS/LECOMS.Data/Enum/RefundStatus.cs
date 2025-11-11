namespace LECOMS.Data.Enum
{
    /// <summary>
    /// Trạng thái của yêu cầu hoàn tiền
    /// Flow: Pending → Approved → Processing → Completed
    ///    hoặc: Pending → Rejected
    /// </summary>
    public enum RefundStatus
    {
        /// <summary>
        /// Chờ admin duyệt
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Admin đã approve, chờ xử lý
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Admin từ chối
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Đang xử lý (cộng/trừ tiền ví)
        /// </summary>
        Processing = 3,

        /// <summary>
        /// Hoàn tất - tiền đã vào ví
        /// </summary>
        Completed = 4,

        /// <summary>
        /// Thất bại - có lỗi khi xử lý
        /// </summary>
        Failed = 5
    }
}