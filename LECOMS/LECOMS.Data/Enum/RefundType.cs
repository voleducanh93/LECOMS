namespace LECOMS.Data.Enum
{
    /// <summary>
    /// Loại hoàn tiền
    /// </summary>
    public enum RefundType
    {
        /// <summary>
        /// Hoàn toàn bộ số tiền đơn hàng
        /// </summary>
        Full = 0,

        /// <summary>
        /// Hoàn một phần
        /// VD: 1 sản phẩm trong đơn bị lỗi
        /// </summary>
        Partial = 1
    }
}