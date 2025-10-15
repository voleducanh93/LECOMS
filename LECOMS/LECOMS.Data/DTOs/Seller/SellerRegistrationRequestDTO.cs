using System;
using System.ComponentModel.DataAnnotations;

namespace LECOMS.Data.DTOs.Seller
{
    public class SellerRegistrationRequestDTO
    {
        [Required, MaxLength(200)]
        public string ShopName { get; set; }

        [MaxLength(500)]
        public string? ShopDescription { get; set; }

        [Required, Phone]
        public string ShopPhoneNumber { get; set; }

        [Required, MaxLength(500)]
        public string ShopAddress { get; set; }

        public string? BusinessType { get; set; }
        public string? OwnershipDocumentUrl { get; set; }

        // ✅ Thay Category bằng CategoryId
        [Required]
        public string CategoryId { get; set; }

        public bool AcceptedTerms { get; set; }

        // Chủ sở hữu
        public string? OwnerFullName { get; set; }
        public DateTime? OwnerDateOfBirth { get; set; }
        public string? OwnerPersonalIdNumber { get; set; }
        public string? OwnerPersonalIdFrontUrl { get; set; }
        public string? OwnerPersonalIdBackUrl { get; set; }
    }
}
