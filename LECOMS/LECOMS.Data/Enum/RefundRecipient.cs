namespace LECOMS.Data.Enum
{
    /// <summary>
    /// Người nhận tiền hoàn - xác định tiền vào ví nào
    /// </summary>
    public enum RefundRecipient
    {
        /// <summary>
        /// Hoàn tiền cho Customer (vào CustomerWallet)
        /// Use case: Hàng lỗi, shop hủy đơn, giao sai hàng
        /// </summary>
        Customer = 0,

        /// <summary>
        /// Hoàn tiền cho Shop (vào ShopWallet)
        /// Use case: Customer hủy đơn, customer từ chối nhận hàng
        /// </summary>
        Shop = 1
    }
}