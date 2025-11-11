namespace LECOMS.Data.Enum
{
    /// <summary>
    /// Lý do hoàn tiền - đơn giản hóa, chi tiết ghi vào ReasonDescription
    /// Enum này giúp: Filter, Report, Auto-determine Recipient
    /// </summary>
    public enum RefundReason
    {
        // ============ HOÀN TIỀN CHO CUSTOMER ============

        /// <summary>
        /// Vấn đề từ Shop
        /// VD: Hàng lỗi, sai hàng, giao trễ, không đúng mô tả
        /// → Hoàn vào CustomerWallet
        /// → Shop chịu trách nhiệm
        /// </summary>
        ShopIssue = 1,

        /// <summary>
        /// Shop hủy đơn hàng
        /// → Hoàn vào CustomerWallet
        /// </summary>
        ShopCancelled = 2,

        // ============ HOÀN TIỀN CHO SHOP ============

        /// <summary>
        /// Customer hủy đơn hoặc từ chối nhận hàng
        /// → Hoàn vào ShopWallet
        /// → Customer chịu trách nhiệm
        /// </summary>
        CustomerCancelled = 10,

        /// <summary>
        /// Đơn hàng gian lận/spam
        /// → Hoàn vào ShopWallet
        /// </summary>
        FraudulentOrder = 11,

        // ============ KHÁC ============

        /// <summary>
        /// Lý do đặc biệt khác
        /// Admin tự quyết định Recipient
        /// BẮT BUỘC phải có mô tả chi tiết
        /// </summary>
        Other = 99
    }
}