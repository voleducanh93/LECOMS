using System;

namespace LECOMS.Data.DTOs.Seller
{
    public class ShopDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }

        public string? BusinessType { get; set; }
        public string? OwnershipDocumentUrl { get; set; }
        public string? Category { get; set; }

        public string Status { get; set; }
        public string? RejectedReason { get; set; }

        public string? OwnerFullName { get; set; }
        public DateTime? OwnerDateOfBirth { get; set; }
        public string? OwnerPersonalIdNumber { get; set; }
        public string? OwnerPersonalIdFrontUrl { get; set; }
        public string? OwnerPersonalIdBackUrl { get; set; }

        public string SellerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
