namespace LECOMS.Data.Enum
{
    /// <summary>
    /// Lý do Customer yêu cầu hoàn tiền
    /// Simple enum - chi tiết trong ReasonDescription
    /// </summary>
    public enum RefundReason
    {
        /// <summary>
        /// Vấn đề về sản phẩm
        /// Bao gồm: lỗi, sai hàng, không đúng mô tả, giao trễ, thiếu phụ kiện
        /// </summary>
        ProductIssue = 0,

        /// <summary>
        /// Lý do khác
        /// BẮT BUỘC mô tả chi tiết ≥20 ký tự
        /// </summary>
        Other = 99
    }
}