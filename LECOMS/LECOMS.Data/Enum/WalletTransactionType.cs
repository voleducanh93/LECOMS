namespace LECOMS.Data.Enum
{
    /// <summary>
    /// Loại giao dịch trong ví - cho audit trail
    /// </summary>
    public enum WalletTransactionType
    {
        /// <summary>
        /// Doanh thu từ đơn hàng (cộng tiền)
        /// Vào ShopWallet.PendingBalance
        /// </summary>
        OrderRevenue = 1,

        /// <summary>
        /// Phí sàn (trừ tiền) - không áp dụng cho shop wallet
        /// Dùng cho reporting platform revenue
        /// </summary>
        PlatformFee = 2,

        /// <summary>
        /// Rút tiền (trừ tiền)
        /// Từ AvailableBalance
        /// </summary>
        Withdrawal = 3,

        /// <summary>
        /// Hoàn tiền (cộng hoặc trừ tùy context)
        /// Customer nhận: cộng vào CustomerWallet
        /// Shop bị trừ: trừ từ ShopWallet
        /// </summary>
        Refund = 4,

        /// <summary>
        /// Điều chỉnh số dư (admin manual adjustment)
        /// </summary>
        Adjustment = 5,

        /// <summary>
        /// Thanh toán đơn hàng (cho customer wallet)
        /// Trừ tiền từ CustomerWallet để mua hàng
        /// </summary>
        Payment = 6,

        /// <summary>
        /// Release balance (chuyển từ Pending sang Available)
        /// Sau khi qua holding period
        /// </summary>
        BalanceRelease = 7
    }
}