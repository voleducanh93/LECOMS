using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Common.Helper
{
    public class EmailSetting
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}
