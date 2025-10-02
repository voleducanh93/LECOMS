using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        [Precision(18, 2)] public decimal Price { get; set; }
        public int Stock { get; set; }
        public byte Active { get; set; } = 1;

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<CourseProduct> CourseProducts { get; set; } = new List<CourseProduct>();
    }
}
