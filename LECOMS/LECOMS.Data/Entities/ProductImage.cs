using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class ProductImage
    {
        [Key] public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required] public string ProductId { get; set; } = null!;
        [ForeignKey(nameof(ProductId))] public Product Product { get; set; } = null!;

        [Required, MaxLength(1000)] public string Url { get; set; } = null!;
        public int OrderIndex { get; set; } = 0;
        public bool IsPrimary { get; set; } = false;
    }
}
