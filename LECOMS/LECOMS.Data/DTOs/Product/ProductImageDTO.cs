using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Product
{
    public class ProductImageDTO
    {
        public string Url { get; set; } = null!;
        public int OrderIndex { get; set; } = 0;
        public bool IsPrimary { get; set; } = false;
    }
}
