using System.ComponentModel.DataAnnotations;

namespace LECOMS.Data.DTOs.Seller
{
    public class RejectShopRequest
    {
        [Required(ErrorMessage = "Rejection reason is required")]
        [MaxLength(500)]
        public string Reason { get; set; }
    }
}
