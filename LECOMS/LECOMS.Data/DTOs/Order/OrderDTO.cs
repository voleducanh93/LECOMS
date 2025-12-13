using System;
using System.Collections.Generic;

namespace LECOMS.Data.DTOs.Order
{
    //public class OrderDTO
    //{
    //    public string Id { get; set; } = null!;
    //    public string OrderCode { get; set; } = null!;
    //    public string UserId { get; set; } = null!;
    //    public int ShopId { get; set; }
    //    public string? ShopName { get; set; }
    //    public string? CustomerName { get; set; }
    //    public string ShipToName { get; set; } = null!;
    //    public string ShipToPhone { get; set; } = null!;
    //    public string ShipToAddress { get; set; } = null!;
    //    public decimal Subtotal { get; set; }
    //    public decimal ShippingFee { get; set; }
    //    public decimal Discount { get; set; }
    //    public decimal Total { get; set; }
    //    public string Status { get; set; } = null!;
    //    public string PaymentStatus { get; set; } = null!;
    //    public bool BalanceReleased { get; set; }
    //    public DateTime CreatedAt { get; set; }
    //    public DateTime? CompletedAt { get; set; }

    //    public List<OrderDetailDTO> Details { get; set; } = new();
    //}
    public class OrderDTO
    {
        public string Id { get; set; } = null!;
        public string OrderCode { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public int ShopId { get; set; }
        public string? ShopName { get; set; }
        public string? CustomerName { get; set; }

        public string ShipToName { get; set; } = null!;
        public string ShipToPhone { get; set; } = null!;
        public string ShipToAddress { get; set; } = null!;

        // ⭐ THÊM:  Thông tin địa chỉ GHN
        public int? ToProvinceId { get; set; }
        public string? ToProvinceName { get; set; }
        public int ToDistrictId { get; set; }
        public string? ToDistrictName { get; set; }
        public string? ToWardCode { get; set; }
        public string? ToWardName { get; set; }

        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }

        public string Status { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public bool BalanceReleased { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // ⭐ THÊM: Thông tin giao hàng
        public string? EstimatedDeliveryText { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public string? ShippingTrackingCode { get; set; }
        public string? ShippingStatus { get; set; }

        public List<OrderDetailDTO> Details { get; set; } = new();
    }
}
