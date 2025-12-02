using LECOMS.Data.DTOs.Chat;
using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IChatService
    {
        // START CHAT
        Task<ConversationDTO> StartSellerConversation(string buyerId, string productId);
        Task<ConversationDTO> StartAIConversation(string buyerId, string productId);

        // SEND MESSAGE
        Task<MessageDTO> SendSellerMessage(Guid conversationId, string senderId, string content);
        Task<MessageDTO> SendAIMessage(Guid conversationId, string senderId, string content); // có thể không dùng, nhưng không sao
        Task<MessageDTO> SendAIUserMessage(Guid conversationId, string senderId, string content);
        Task<MessageDTO> SendAIReply(Guid conversationId, string senderId, string content);

        // GET MESSAGES
        Task<IEnumerable<MessageDTO>> GetMessages(Guid conversationId, string currentUserId);

        // LIST CONVERSATIONS
        Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId);
        Task<IEnumerable<Conversation>> GetSellerConversationsAsync(string sellerId);

        // GET SINGLE CONVERSATION
        Task<ConversationDTO> GetUserConversationById(Guid conversationId, string userId);
        Task<ConversationDTO> GetSellerConversationById(Guid conversationId, string sellerId);

        // SUMMARY cho realtime danh sách
        Task<ConversationDTO> GetConversationSummary(Guid conversationId);
    }
}
