using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Voucher
{
    /// <summary>
    /// Dùng cho API preview voucher trước khi checkout
    /// FE gửi lên danh sách "đơn dự kiến" (theo shop) với Subtotal + ShippingFee
    /// </summary>
    public class OrderPreviewItemDTO
    {
        /// <summary>
        /// Id tạm / id thật đều được, chỉ dùng để map discount per order
        /// </summary>
        public string OrderId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Tổng tiền hàng (chưa gồm shipping, chưa trừ discount)
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Phí ship dự kiến cho đơn đó
        /// </summary>
        public decimal ShippingFee { get; set; }
    }

    public class VoucherPreviewRequestDTO
    {
        /// <summary>Mã voucher mà user nhập</summary>
        public string VoucherCode { get; set; } = null!;

        /// <summary>
        /// Danh sách các "đơn dự kiến" (mỗi shop một đơn)
        /// </summary>
        public List<OrderPreviewItemDTO> Orders { get; set; } = new();
    }
}
