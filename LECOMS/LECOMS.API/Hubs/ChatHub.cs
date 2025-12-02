using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace LECOMS.API.Hubs
{
    public class ChatHub : Hub
    {
        /// <summary>
        /// Tham gia một cuộc trò chuyện cụ thể (room theo ConversationId)
        /// </summary>
        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"CONV_{conversationId}");
        }

        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"CONV_{conversationId}");
        }

        /// <summary>
        /// Tham gia group theo UserId để nhận realtime danh sách cuộc trò chuyện
        /// </summary>
        public async Task JoinUser(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"USER_{userId}");
        }

        public async Task LeaveUser(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"USER_{userId}");
        }
    }
}
