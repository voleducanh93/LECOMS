using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    public class Conversation
    {
        [Key]
        public Guid Id { get; set; }

        public string BuyerId { get; set; }
        public string? SellerId { get; set; }   // Nullable khi chat AI
        public string ProductId { get; set; }

        public bool IsAIChat { get; set; } = false;

        public string? LastMessage { get; set; }
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual User Buyer { get; set; }
        public virtual User? Seller { get; set; }
        public virtual Product Product { get; set; }

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
