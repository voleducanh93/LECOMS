using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Refund
{
    /// <summary>
    /// DTO thống kê tổng quan Refund cho Admin Dashboard
    /// </summary>
    public class RefundDashboardDTO
    {
        public int TotalRequests { get; set; }

        public int PendingShop { get; set; }
        public int PendingAdmin { get; set; }
        public int ShopRejected { get; set; }
        public int AdminRejected { get; set; }
        public int Refunded { get; set; }

        public decimal TotalRefundAmount { get; set; }

        public int RefundedLast30DaysCount { get; set; }
        public decimal RefundedLast30DaysAmount { get; set; }

        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
