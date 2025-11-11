using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Refund
{
    public class RefundRequestDTO
    {
        public string Id { get; set; } = null!;
        public string OrderId { get; set; } = null!;
        public string? OrderCode { get; set; }
        public decimal RefundAmount { get; set; }
        public string Recipient { get; set; } = null!;
        public string ReasonType { get; set; } = null!;
        public string ReasonDescription { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string RequestedBy { get; set; } = null!;
        public string? RequestedByName { get; set; }
        public DateTime RequestedAt { get; set; }
        public string? ProcessedBy { get; set; }
        public string? ProcessedByName { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
