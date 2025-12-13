using LECOMS.Data.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    /// <summary>
    /// Entity đơn hàng - SÀN THU HỘ
    /// </summary>
    [Index(nameof(UserId), nameof(CreatedAt))]
    [Index(nameof(ShopId), nameof(CreatedAt))]
    [Index(nameof(OrderCode), IsUnique = true)]
    [Index(nameof(Status))]
    [Index(nameof(PaymentStatus))]
    public class Order
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Mã đơn hàng hiển thị cho user
        /// Format: ORD{YYYYMMDDHHMMSS}{Random}
        /// VD: ORD20251111040530001
        /// </summary>
        [Required, MaxLength(50)]
        public string OrderCode { get; set; } = null!;

        // ============ CUSTOMER INFO ============

        /// <summary>
        /// ID của customer đặt hàng
        /// </summary>
        [Required]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        // ============ SHOP INFO ============

        /// <summary>
        /// ID của shop bán hàng
        /// </summary>
        [Required]
        public int ShopId { get; set; }

        [ForeignKey(nameof(ShopId))]
        public Shop Shop { get; set; } = null!;

        // ============ SHIPPING INFO ============

        [Required, MaxLength(255)]
        public string ShipToName { get; set; } = null!;

        [Required, MaxLength(20)]
        public string ShipToPhone { get; set; } = null!;

        [Required, MaxLength(500)]
        public string ShipToAddress { get; set; } = null!;

        // ⭐⭐⭐ THÊM MỚI:  Địa chỉ GHN Format (SỬA TẤT CẢ LỖI CS0117)
        /// <summary>
        /// Mã tỉnh/thành phố (GHN ProvinceID)
        /// </summary>
        public int? ToProvinceId { get; set; }

        [MaxLength(200)]
        public string? ToProvinceName { get; set; }

        /// <summary>
        /// Mã quận/huyện (GHN DistrictID) - BẮT BUỘC để tính ship
        /// </summary>
        public int ToDistrictId { get; set; }

        [MaxLength(200)]
        public string? ToDistrictName { get; set; }

        /// <summary>
        /// Mã phường/xã (GHN WardCode) - BẮT BUỘC để tính ship
        /// </summary>
        [Required, MaxLength(50)]
        public string ToWardCode { get; set; } = null!;

        [MaxLength(200)]
        public string? ToWardName { get; set; }

        /// <summary>
        /// Service Type:  2 = Express (nhanh), 5 = Standard (tiêu chuẩn)
        /// </summary>
        public int ServiceTypeId { get; set; } = 2;

        // ============ PRICING ============

        /// <summary>
        /// Tổng tiền hàng (chưa bao gồm phí ship, discount)
        /// </summary>
        [Precision(18, 2)]
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Phí vận chuyển
        /// </summary>
        [Precision(18, 2)]
        public decimal ShippingFee { get; set; }

        /// <summary>
        /// Giảm giá (voucher, promotion)
        /// </summary>
        [Precision(18, 2)]
        public decimal Discount { get; set; }

        /// <summary>
        /// Tổng thanh toán = Subtotal + ShippingFee - Discount
        /// </summary>
        [Precision(18, 2)]
        public decimal Total { get; set; }

        // ============ STATUS ============

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        // ============ TIMESTAMPS ============

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời gian hoàn thành đơn (customer xác nhận nhận hàng)
        /// Dùng để tính holding period
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        //============= Thời gian giao hàng dự kiến ================
        /// <summary>
        /// Thời gian dự kiến giao hàng (từ GHN API)
        /// </summary>
        public DateTime? EstimatedDeliveryDate { get; set; }

        /// <summary>
        /// Mô tả thời gian giao hàng (VD: "2-3 ngày")
        /// </summary>
        [MaxLength(100)]
        public string? EstimatedDeliveryText { get; set; }

        // ⭐ Mã vận đơn tracking
        /// <summary>
        /// Mã vận đơn từ GHN (order_code) để tracking
        /// </summary>
        [MaxLength(100)]
        public string? ShippingTrackingCode { get; set; }

        /// <summary>
        /// Trạng thái vận đơn từ GHN
        /// </summary>
        [MaxLength(50)]
        public string? ShippingStatus { get; set; }

        // ============ WALLET MANAGEMENT ============

        /// <summary>
        /// Đã release balance cho shop chưa?
        /// false = Tiền còn trong PendingBalance
        /// true = Tiền đã chuyển sang AvailableBalance
        /// </summary>
        public bool BalanceReleased { get; set; } = false;

        /// <summary>Voucher áp dụng cho đơn (nếu có)</summary>
        [MaxLength(50)]
        public string? VoucherCodeUsed { get; set; }

        // ============ NAVIGATION PROPERTIES ============

        public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();
        public ICollection<TransactionOrder> TransactionOrders { get; set; } = new List<TransactionOrder>();
    }
}