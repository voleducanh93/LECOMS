using LECOMS.API.Hubs;
using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Chat;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]  // Buyer hoặc Seller đều cần login
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly UserManager<User> _userManager;
        private readonly IHubContext<ChatHub> _hub;

        public ChatController(
            IChatService chatService,
            UserManager<User> userManager,
            IHubContext<ChatHub> hub)
        {
            _chatService = chatService;
            _userManager = userManager;
            _hub = hub;
        }

        // ================================================
        // START SELLER CHAT
        // ================================================
        [HttpPost("seller/start")]
        public async Task<IActionResult> StartSellerChat([FromBody] StartChatDTO dto)
        {
            var userId = _userManager.GetUserId(User);

            var conv = await _chatService.StartSellerConversation(userId, dto.ProductId);
            return Ok(conv);
        }

        // ================================================
        // START AI CHAT
        // ================================================
        [HttpPost("ai/start")]
        public async Task<IActionResult> StartAIChat([FromBody] StartChatDTO dto)
        {
            var userId = _userManager.GetUserId(User);

            var conv = await _chatService.StartAIConversation(userId, dto.ProductId);
            return Ok(conv);
        }

        // ================================================
        // SEND MESSAGE TO SELLER
        // ================================================
        [HttpPost("{conversationId}/message")]
        public async Task<IActionResult> SendMessage(Guid conversationId, [FromBody] SendMessageDTO dto)
        {
            var userId = _userManager.GetUserId(User);

            var msg = await _chatService.SendSellerMessage(conversationId, userId, dto.Content); // hoặc rename thành SendMessage

            await _hub.Clients.Group(conversationId.ToString())
                .SendAsync("ReceiveMessage", msg);

            return Ok(msg);
        }


        // ================================================
        // SEND MESSAGE TO AI (AI auto reply)
        // ================================================
        [HttpPost("ai/{conversationId}/message")]
        public async Task<IActionResult> SendAIMessage(Guid conversationId, [FromBody] SendMessageDTO dto)
        {
            var userId = _userManager.GetUserId(User);

            var aiMsg = await _chatService.SendAIMessage(conversationId, userId, dto.Content);

            return Ok(aiMsg);
        }

        // ================================================
        // GET ALL MESSAGES
        // ================================================
        [HttpGet("{conversationId}/messages")]
        public async Task<IActionResult> GetMessages(Guid conversationId)
        {
            var msgs = await _chatService.GetMessages(conversationId);
            return Ok(msgs);
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserConversations()
        {
            var userId = User.FindFirst("uid")?.Value;
            var data = await _chatService.GetUserConversationsAsync(userId);
            return Ok(data);
        }

        [HttpGet("seller")]
        public async Task<IActionResult> GetSellerConversations()
        {
            var sellerId = User.FindFirst("uid")?.Value;
            var data = await _chatService.GetSellerConversationsAsync(sellerId);
            return Ok(data);
        }

    }
}
