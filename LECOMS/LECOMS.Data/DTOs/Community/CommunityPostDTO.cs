using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Community
{
    public class CommunityPostDTO
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserSimpleDTO User { get; set; }
        public IEnumerable<CommentDTO> Comments { get; set; }
    }

}
