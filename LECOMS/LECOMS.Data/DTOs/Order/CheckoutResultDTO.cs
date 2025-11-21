using System.Collections.Generic;

namespace LECOMS.Data.DTOs.Order
{
    public class CheckoutResultDTO
    {
        public List<OrderDTO> Orders { get; set; } = new();

        public string? PaymentUrl { get; set; }

        public decimal TotalAmount { get; set; }

        // Breakdown
        public string PaymentMethod { get; set; } = null!;
        public decimal WalletAmountUsed { get; set; }
        public decimal PayOSAmountRequired { get; set; }
        public decimal DiscountApplied { get; set; }
        public decimal ShippingFee { get; set; }
        public string? VoucherCodeUsed { get; set; }
    }
}
