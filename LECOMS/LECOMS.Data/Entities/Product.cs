﻿using LECOMS.Data.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    [Index(nameof(Slug), IsUnique = true)]
    public class Product
    {
        [Key] public string Id { get; set; }

        [Required, MaxLength(200)] public string Name { get; set; } = null!;
        [Required, MaxLength(220)] public string Slug { get; set; } = null!;
        [MaxLength(1000)] public string? Description { get; set; }

        [Required] public string CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))] public ProductCategory Category { get; set; } = null!;

        // ✅ Thêm liên kết với Shop
        [Required] public int ShopId { get; set; }
        [ForeignKey(nameof(ShopId))] public Shop Shop { get; set; } = null!;

        [Precision(18, 2)] public decimal Price { get; set; }
        public int Stock { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Draft;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public byte Active { get; set; } = 1;

        // Ảnh sản phẩm
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        // Liên kết cũ với course, sẽ bỏ sau
        public ICollection<CourseProduct> CourseProducts { get; set; } = new List<CourseProduct>();
    }
}
