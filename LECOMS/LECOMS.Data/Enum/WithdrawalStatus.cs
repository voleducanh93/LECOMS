namespace LECOMS.Data.Enum
{
    /// <summary>
    /// Trạng thái yêu cầu rút tiền
    /// Flow: Pending → Approved → Processing → Completed
    ///    hoặc: Pending → Rejected
    /// </summary>
    public enum WithdrawalStatus
    {
        /// <summary>
        /// Chờ admin duyệt
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Admin đã approve
        /// Tiền đã trừ khỏi wallet
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Admin từ chối
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Đang xử lý chuyển khoản ngân hàng
        /// </summary>
        Processing = 3,

        /// <summary>
        /// Đã chuyển khoản thành công
        /// </summary>
        Completed = 4,

        /// <summary>
        /// Chuyển khoản thất bại
        /// Cần hoàn tiền vào wallet
        /// </summary>
        Failed = 5
    }
}