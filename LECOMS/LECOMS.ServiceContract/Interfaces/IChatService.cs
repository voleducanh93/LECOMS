using LECOMS.Data.DTOs.Chat;
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

        Task<IEnumerable<MessageDTO>> GetMessages(Guid conversationId);
    }
}
