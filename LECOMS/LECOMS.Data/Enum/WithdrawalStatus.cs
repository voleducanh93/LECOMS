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
        /// Đã chuyển khoản thành công
        /// </summary>
        Completed = 3,

    }
}