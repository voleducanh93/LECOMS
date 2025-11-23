using LECOMS.Data.DTOs.Chat;
using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IChatService
    {
        Task<ConversationDTO> StartSellerConversation(string buyerId, string productId);
        Task<ConversationDTO> StartAIConversation(string buyerId, string productId);

        Task<MessageDTO> SendSellerMessage(Guid conversationId, string senderId, string content);
        Task<MessageDTO> SendAIMessage(Guid conversationId, string senderId, string content);

        Task<IEnumerable<MessageDTO>> GetMessages(Guid conversationId, string currentUserId);
        Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId);
        Task<IEnumerable<Conversation>> GetSellerConversationsAsync(string sellerId);
        Task<ConversationDTO> GetUserConversationById(Guid conversationId, string userId);
        Task<ConversationDTO> GetSellerConversationById(Guid conversationId, string sellerId);


    }
}
