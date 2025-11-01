using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Order
{
    public class CheckoutRequestDTO
    {
        public string ShipToName { get; set; } = null!;
        public string ShipToPhone { get; set; } = null!;
        public string ShipToAddress { get; set; } = null!;

        // optional fields (coupon, shipping method) can be added later
    }
}
