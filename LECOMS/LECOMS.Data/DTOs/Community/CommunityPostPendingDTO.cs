using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Community
{
    public class CommunityPostPendingDTO
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
