using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    public class Shop
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [MaxLength(500)]
        public string Address { get; set; }

        public string? BusinessType { get; set; }
        public string? OwnershipDocumentUrl { get; set; }

        // ✅ Liên kết category (Admin tạo)
        [Required]
        public string CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public CourseCategory Category { get; set; }

        public bool AcceptedTerms { get; set; } = false;

        // Chủ sở hữu
        public string? OwnerFullName { get; set; }
        public DateTime? OwnerDateOfBirth { get; set; }
        public string? OwnerPersonalIdNumber { get; set; }
        public string? OwnerPersonalIdFrontUrl { get; set; }
        public string? OwnerPersonalIdBackUrl { get; set; }

        public string Status { get; set; } = "Pending";
        public string? RejectedReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }

        public string SellerId { get; set; }
        [ForeignKey("SellerId")]
        public virtual User Seller { get; set; }
    }
}