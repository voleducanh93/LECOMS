using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Chat
{
    public class ConversationDTO
    {
        public Guid Id { get; set; }
        public bool IsAIChat { get; set; }

        public string BuyerId { get; set; }
        public string? SellerId { get; set; }

        public ProductMiniDTO Product { get; set; }

        public string LastMessage { get; set; }
        public DateTime LastMessageAt { get; set; }
    }

}
