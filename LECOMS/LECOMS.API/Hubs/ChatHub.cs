using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace LECOMS.API.Hubs
{
    public class ChatHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            // FE sẽ join vào room theo conversationId
            return base.OnConnectedAsync();
        }

        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }
    }
}
