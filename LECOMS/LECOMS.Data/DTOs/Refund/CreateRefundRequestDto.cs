using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Refund
{
    /// <summary>
    /// DTO để customer tạo refund request
    /// </summary>
    public class CreateRefundRequestDto
    {
        public string OrderId { get; set; } = null!;
        public string RequestedBy { get; set; } = null!;
        public RefundReason ReasonType { get; set; }
        public string ReasonDescription { get; set; } = null!;
        public RefundType Type { get; set; }
        public decimal RefundAmount { get; set; }
        public string? AttachmentUrls { get; set; }
    }
}
