using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    public class Message
    {
        [Key] public Guid Id { get; set; }

        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; }

        public string SenderId { get; set; }   // string tự do

        public string Content { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // OPTIONAL: Sender không FK
        [ForeignKey(nameof(SenderId))]
        public virtual User? Sender { get; set; }   // CHO PHÉP NULL
    }


}
