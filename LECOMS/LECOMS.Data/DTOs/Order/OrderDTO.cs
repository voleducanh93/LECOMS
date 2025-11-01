﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Order
{
    public class OrderDTO
    {
        public string Id { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string ShipToName { get; set; } = null!;
        public string ShipToPhone { get; set; } = null!;
        public string ShipToAddress { get; set; } = null!;
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<OrderDetailDTO> Details { get; set; } = new();
    }
}
