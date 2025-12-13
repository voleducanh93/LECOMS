using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Shop
{
    public class ConnectGHNRequestDTO
    {
        [Required]
        public string GHNToken { get; set; } = null!;

        [Required]
        public string GHNShopId { get; set; } = null!;
    }
}
