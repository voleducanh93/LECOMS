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

        public string SenderId { get; set; }   // chỉ là string, KHÔNG FK

        public string Content { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // KHÔNG NAVIGATION TỚI USER
        public User? Sender { get; set; }   // XÓA thuộc tính này nếu được
    }



}
