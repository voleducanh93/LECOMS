using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    public class Shop
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Phone]
        public string PhoneNumber { get; set; } = null!;

        [MaxLength(500)]
        public string Address { get; set; } = null!;

        public string? BusinessType { get; set; }
        public string? OwnershipDocumentUrl { get; set; }

        [MaxLength(1000)]
        public string? ShopAvatar { get; set; }

        [MaxLength(1000)]
        public string? ShopBanner { get; set; }

        [MaxLength(1000)]
        public string? ShopFacebook { get; set; }

        [MaxLength(1000)]
        public string? ShopTiktok { get; set; }

        [MaxLength(1000)]
        public string? ShopInstagram { get; set; }

        [Required]
        public string CategoryId { get; set; } = null!;

        [ForeignKey(nameof(CategoryId))]
        public CourseCategory Category { get; set; } = null!;

        public bool AcceptedTerms { get; set; } = false;

        public string? OwnerFullName { get; set; }
        public DateTime? OwnerDateOfBirth { get; set; }
        public string? OwnerPersonalIdNumber { get; set; }
        public string? OwnerPersonalIdFrontUrl { get; set; }
        public string? OwnerPersonalIdBackUrl { get; set; }

        public string Status { get; set; } = "Pending";
        public string? RejectedReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }

        [Required]
        public string SellerId { get; set; } = null!;

        [ForeignKey(nameof(SellerId))]
        public virtual User Seller { get; set; } = null!;

        // ================= GHN CONFIG =================

        /// <summary>
        /// GHN API Token của shop (seller tự cung cấp)
        /// </summary>
        [MaxLength(200)]
        public string? GHNToken { get; set; }

        /// <summary>
        /// GHN ShopId tương ứng với token
        /// </summary>
        [MaxLength(50)]
        public string? GHNShopId { get; set; }

        /// <summary>
        /// Shop đã kết nối GHN hay chưa
        /// </summary>
        [NotMapped]
        public bool IsGHNConnected =>
            !string.IsNullOrWhiteSpace(GHNToken)
            && !string.IsNullOrWhiteSpace(GHNShopId);

        // ============ NAVIGATION PROPERTIES ⭐ MỚI ============

        /// <summary>
        /// Sản phẩm của shop
        /// </summary>
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        /// <summary>
        /// Đơn hàng của shop
        /// </summary>
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}