namespace LECOMS.Data.Enum
{
    /// <summary>
    /// Trạng thái refund request
    /// Simple flow: Shop decides, no admin needed
    /// </summary>
    public enum RefundStatus
    {
        PendingShop = 0,           // Customer gửi → chờ Shop duyệt
        ShopApproved = 1,          // Shop chấp nhận → chờ Admin duyệt
        ShopRejected = 2,          // Shop từ chối → kết thúc
        PendingAdmin = 3,          // Shop approve → chờ Admin
        AdminApproved = 4,         // Admin duyệt → tiến hành hoàn tiền
        AdminRejected = 5,         // Admin từ chối
        Refunded = 6               // Hoàn tiền thành công
    }
}