using System;
using System.ComponentModel.DataAnnotations;

namespace LECOMS.Data.DTOs.Seller
{
    public class ShopUpdateDTO
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public string? BusinessType { get; set; }
        public string? OwnershipDocumentUrl { get; set; }

        // New: clients can pass existing Cloudinary URLs when updating
        public string? ShopAvatar { get; set; }
        public string? ShopBanner { get; set; }
        public string? ShopFacebook { get; set; }
        public string? ShopTiktok { get; set; }
        public string? ShopInstagram { get; set; }

        public string? CategoryId { get; set; }
        public bool? AcceptedTerms { get; set; }

        // Owner info
        public string? OwnerFullName { get; set; }
        public DateTime? OwnerDateOfBirth { get; set; }
        public string? OwnerPersonalIdNumber { get; set; }
        public string? OwnerPersonalIdFrontUrl { get; set; }
        public string? OwnerPersonalIdBackUrl { get; set; }
    }
}