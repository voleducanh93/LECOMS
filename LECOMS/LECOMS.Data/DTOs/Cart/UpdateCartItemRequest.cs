using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Cart
{
    /// <summary>
    /// DTO để update số lượng sản phẩm trong cart
    /// </summary>
    public class UpdateCartItemRequest
    {
        /// <summary>
        /// Set số lượng tuyệt đối (ví dụ: user nhập số 5)
        /// </summary>
        public int? AbsoluteQuantity { get; set; }

        /// <summary>
        /// Tăng/giảm số lượng (ví dụ: +1 hoặc -1)
        /// </summary>
        public int? QuantityChange { get; set; }
    }
}
