using AutoMapper;
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
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly UserManager<User> _userManager;
        private readonly IHubContext<ChatHub> _hub;
        private readonly IMapper _mapper;

        public ChatController(
            IChatService chatService,
            UserManager<User> userManager,
            IHubContext<ChatHub> hub,
            IMapper mapper)
        {
            _chatService = chatService;
            _userManager = userManager;
            _hub = hub;
            _mapper = mapper;
        }

        // --------------------------
        // START CHAT WITH SELLER
        // --------------------------
        [HttpPost("seller/start")]
        public async Task<IActionResult> StartSellerChat([FromBody] StartChatDTO dto)
        {
            var response = new APIResponse();

            try
            {
                var userId = _userManager.GetUserId(User);
                var conv = await _chatService.StartSellerConversation(userId, dto.ProductId);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = conv;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        // --------------------------
        // START CHAT WITH AI
        // --------------------------
        [HttpPost("ai/start")]
        public async Task<IActionResult> StartAIChat([FromBody] StartChatDTO dto)
        {
            var response = new APIResponse();

            try
            {
                var userId = _userManager.GetUserId(User);
                var conv = await _chatService.StartAIConversation(userId, dto.ProductId);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = conv;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        // --------------------------
        // SEND MESSAGE (BUYER <-> SELLER)
        // --------------------------
        [HttpPost("{conversationId}/message")]
        public async Task<IActionResult> SendMessage(Guid conversationId, [FromBody] SendMessageDTO dto)
        {
            var response = new APIResponse();

            try
            {
                var userId = _userManager.GetUserId(User);

                // Lưu message
                var msg = await _chatService.SendSellerMessage(conversationId, userId, dto.Content);

                // 🔥 Broadcast message vào room cuộc trò chuyện
                await _hub.Clients.Group($"CONV_{conversationId}")
                    .SendAsync("ReceiveMessage", msg);

                // 🔥 Lấy summary của conversation để cập nhật danh sách realtime
                var convSummary = await _chatService.GetConversationSummary(conversationId);

                await _hub.Clients.Group($"USER_{convSummary.BuyerId}")
                    .SendAsync("UpdateConversationList", convSummary);

                if (!string.IsNullOrEmpty(convSummary.SellerId))
                {
                    await _hub.Clients.Group($"USER_{convSummary.SellerId}")
                        .SendAsync("UpdateConversationList", convSummary);
                }

                response.StatusCode = HttpStatusCode.OK;
                response.Result = msg;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        // --------------------------
        // SEND MESSAGE TO AI
        // --------------------------
        [HttpPost("ai/{conversationId}/message")]
        public async Task<IActionResult> SendAIMessage(Guid conversationId, [FromBody] SendMessageDTO dto)
        {
            var response = new APIResponse();

            try
            {
                var userId = _userManager.GetUserId(User);

                // 1) Lưu tin nhắn của user
                var userMsg = await _chatService.SendAIUserMessage(conversationId, userId, dto.Content);

                // 🔥 Broadcast tin nhắn USER
                await _hub.Clients.Group($"CONV_{conversationId}")
                    .SendAsync("ReceiveMessage", userMsg);

                // 2) AI trả lời
                var aiMsg = await _chatService.SendAIReply(conversationId, userId, dto.Content);

                // 🔥 Broadcast tin nhắn AI
                await _hub.Clients.Group($"CONV_{conversationId}")
                    .SendAsync("ReceiveMessage", aiMsg);

                // 🔥 Cập nhật danh sách conversation cho buyer
                var convSummary = await _chatService.GetConversationSummary(conversationId);

                await _hub.Clients.Group($"USER_{convSummary.BuyerId}")
                    .SendAsync("UpdateConversationList", convSummary);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = aiMsg;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        // --------------------------
        // GET MESSAGES
        // --------------------------
        [HttpGet("{conversationId}/messages")]
        public async Task<IActionResult> GetMessages(Guid conversationId)
        {
            var response = new APIResponse();
            try
            {
                var userId = _userManager.GetUserId(User);
                var msgs = await _chatService.GetMessages(conversationId, userId);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = msgs;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        // --------------------------
        // GET USER’S CONVERSATIONS
        // --------------------------
        [HttpGet("user")]
        public async Task<IActionResult> GetUserConversations()
        {
            var response = new APIResponse();

            try
            {
                var userId = _userManager.GetUserId(User);
                var data = await _chatService.GetUserConversationsAsync(userId);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = _mapper.Map<IEnumerable<ConversationDTO>>(data);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        // --------------------------
        // GET SELLER’S CONVERSATIONS
        // --------------------------
        [HttpGet("seller")]
        public async Task<IActionResult> GetSellerConversations()
        {
            var response = new APIResponse();

            try
            {
                var sellerId = _userManager.GetUserId(User);
                var data = await _chatService.GetSellerConversationsAsync(sellerId);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = _mapper.Map<IEnumerable<ConversationDTO>>(data);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        // --------------------------
        // GET 1 CONVERSATION (BUYER)
        // --------------------------
        [HttpGet("user/{conversationId}")]
        public async Task<IActionResult> GetUserConversation(Guid conversationId)
        {
            var response = new APIResponse();

            try
            {
                var userId = _userManager.GetUserId(User);

                var conv = await _chatService.GetUserConversationById(conversationId, userId);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = conv;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        // --------------------------
        // GET 1 CONVERSATION (SELLER)
        // --------------------------
        [HttpGet("seller/{conversationId}")]
        public async Task<IActionResult> GetSellerConversation(Guid conversationId)
        {
            var response = new APIResponse();

            try
            {
                var sellerId = _userManager.GetUserId(User);

                var conv = await _chatService.GetSellerConversationById(conversationId, sellerId);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = conv;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }
    }
}
