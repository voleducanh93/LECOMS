using LECOMS.Data.Enum;
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
        public string Name { get; set; }
        public string CategoryId { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public ProductStatus? Status { get; set; } // optional, default Draft
        public List<ProductImageDTO>? Images { get; set; } // optional
    }
}
