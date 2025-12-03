using System;
using System.ComponentModel.DataAnnotations;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Entity cấu hình toàn platform (Singleton pattern)
    /// Chỉ có 1 record duy nhất trong database
    /// </summary>
    public class PlatformConfig
    {
        /// <summary>
        /// Primary Key - Singleton ID
        /// </summary>
        [Key]
        [MaxLength(100)]
        public string Id { get; set; } = "PLATFORM_CONFIG_SINGLETON";

        // ============ COMMISSION & FEES ============

        /// <summary>
        /// Phí hoa hồng mặc định cho platform (%)
        /// Default: 5%
        /// </summary>
        public decimal DefaultCommissionRate { get; set; } = 5.00m;

        // ============ ORDER SETTINGS ============

        /// <summary>
        /// Số ngày giữ tiền trong PendingBalance trước khi release cho shop
        /// Default: 7 days
        /// </summary>
        public int OrderHoldingDays { get; set; } = 7;

        // ============ WITHDRAWAL SETTINGS ============

        /// <summary>
        /// Số tiền rút tối thiểu (VND)
        /// Default: 100,000 VND
        /// </summary>
        public decimal MinWithdrawalAmount { get; set; } = 100000m;

        /// <summary>
        /// Số tiền rút tối đa (VND)
        /// Default: 50,000,000 VND
        /// </summary>
        public decimal MaxWithdrawalAmount { get; set; } = 50000000m;

        /// <summary>
        /// Tự động approve Yêu cầu rút tiền
        /// Default: false (cần admin approve)
        /// </summary>
        public bool AutoApproveWithdrawal { get; set; } = false;

        // ============ REFUND SETTINGS ============

        /// <summary>
        /// Số ngày tối đa để refund kể từ ngày hoàn thành order
        /// Default: 30 days
        /// </summary>
        public int MaxRefundDays { get; set; } = 30;

        /// <summary>
        /// Tự động approve refund requests
        /// Default: false (cần admin approve)
        /// </summary>
        public bool AutoApproveRefund { get; set; } = false;

        /// <summary>
        /// SellerRefundResponseHours
        /// </summary>
        public int SellerRefundResponseHours { get; set; }

        // ============ PAYOS SETTINGS ⭐ MỚI ============

        /// <summary>
        /// PayOS Environment: "sandbox" hoặc "production"
        /// </summary>
        [MaxLength(50)]
        public string PayOSEnvironment { get; set; } = "sandbox";

        /// <summary>
        /// PayOS Client ID
        /// </summary>
        [MaxLength(255)]
        public string? PayOSClientId { get; set; }

        /// <summary>
        /// PayOS API Key (sensitive)
        /// </summary>
        [MaxLength(255)]
        public string? PayOSApiKey { get; set; }

        /// <summary>
        /// PayOS Checksum Key (sensitive)
        /// </summary>
        [MaxLength(255)]
        public string? PayOSChecksumKey { get; set; }

        // ============ NOTIFICATION SETTINGS ⭐ MỚI ============

        /// <summary>
        /// Bật/tắt email notification
        /// </summary>
        public bool EnableEmailNotification { get; set; } = true;

        /// <summary>
        /// Bật/tắt SMS notification
        /// </summary>
        public bool EnableSMSNotification { get; set; } = false;

        // ============ METADATA ============

        /// <summary>
        /// Thời gian cập nhật cuối cùng
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Admin ID đã cập nhật cuối cùng
        /// </summary>
        [MaxLength(450)]
        public string? LastUpdatedBy { get; set; }
    }
}