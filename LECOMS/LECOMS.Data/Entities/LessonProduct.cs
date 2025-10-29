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
    [Index(nameof(LessonId), nameof(ProductId), IsUnique = true)]
    public class LessonProduct
    {
        [Key] public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required] public string LessonId { get; set; } = null!;
        [ForeignKey(nameof(LessonId))] public Lesson Lesson { get; set; } = null!;

        [Required] public string ProductId { get; set; } = null!;
        [ForeignKey(nameof(ProductId))] public Product Product { get; set; } = null!;
    }
}
