using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Product
{
    public class ProductCreateDTO
    {
        [Required, MaxLength(200)]
        public string Name { get; set; }
        [MaxLength(1000)]
        public string? Description { get; set; }
        [Required]
        public string CategoryId { get; set; }
        [Required, Range(0, double.MaxValue)]
        public decimal Price { get; set; }
        [Required, Range(0, int.MaxValue)]
        public int Stock { get; set; }
    }
}
