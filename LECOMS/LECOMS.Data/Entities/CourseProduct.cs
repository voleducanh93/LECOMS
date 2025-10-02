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
    [Index(nameof(CourseId), nameof(ProductId), IsUnique = true)]
    public class CourseProduct
    {
        [Key] public string Id { get; set; }

        [Required] public string CourseId { get; set; }
        [ForeignKey(nameof(CourseId))] public Course Course { get; set; } = null!;

        [Required] public string ProductId { get; set; }
        [ForeignKey(nameof(ProductId))] public Product Product { get; set; } = null!;
    }
}
