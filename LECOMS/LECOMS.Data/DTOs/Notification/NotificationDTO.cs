using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Notification
{
    public class NotificationDTO
    {
        public string Id { get; set; }
        public string Type { get; set; } = "System";
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
