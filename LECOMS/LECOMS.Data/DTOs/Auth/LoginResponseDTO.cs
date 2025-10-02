using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Auth
{
    public class LoginResponseDTO
    {
        public string Token { get; set; }
        public string RefeshToken { get; set; }
        public string UserId { get; set; }
    }
}
