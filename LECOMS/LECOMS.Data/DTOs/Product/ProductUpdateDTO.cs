using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Product
{
    public class ProductUpdateDTO
    {
        [MaxLength(200)]
        public string? Name { get; set; }
        [MaxLength(1000)]
        public string? Description { get; set; }
        public string? CategoryId { get; set; }
        public decimal? Price { get; set; }
        public int? Stock { get; set; }
    }
}
