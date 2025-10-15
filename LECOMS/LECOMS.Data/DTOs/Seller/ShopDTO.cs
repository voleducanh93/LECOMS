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

            // ✅ Đổi Category → CategoryId, thêm tên category nếu muốn hiển thị
            public string CategoryId { get; set; }
            public string? CategoryName { get; set; }  // lấy từ CourseCategory.Name (option)

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