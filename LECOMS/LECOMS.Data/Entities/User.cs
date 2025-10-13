using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LECOMS.Data.Entities
{
    public class User : IdentityUser
    {
        [MaxLength(150)]
        public string? FullName { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public string? CertificateImageUrl { get; set; }
        public virtual Shop Shop { get; set; }

        // Navigation
        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
        public ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();
        public ICollection<CommunityPost> Posts { get; set; } = new List<CommunityPost>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
