using AutoMapper;
using LECOMS.Data.DTOs.Chat;
using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAIProductChatService _ai;

        public ChatService(
            IUnitOfWork uow,
            IMapper mapper,
            IAIProductChatService aiProductChatService)
        {
            _uow = uow;
            _mapper = mapper;
            _ai = aiProductChatService;
        }


        // ========================================================
        // 1. START Chat with Seller
        // ========================================================
        public async Task<ConversationDTO> StartSellerConversation(string buyerId, string productId)
        {
            var product = await _uow.Products.GetAsync(
                p => p.Id == productId,
                includeProperties: "Shop,Images,Category");

            if (product == null)
                throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

            var sellerId = product.Shop.SellerId;

            if (buyerId == sellerId)
                throw new InvalidOperationException("Người bán không thể trò chuyện với chính mình.");

            var existing = await _uow.Conversations.GetByKeyAsync(
                buyerId,
                sellerId,
                productId,
                isAI: false
            );

            if (existing != null)
                return _mapper.Map<ConversationDTO>(existing);

            var conv = new Conversation
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                SellerId = sellerId,
                ProductId = productId,
                IsAIChat = false,
                LastMessage = "",
                LastMessageAt = DateTime.UtcNow
            };

            await _uow.Conversations.AddAsync(conv);
            await _uow.CompleteAsync();

            return _mapper.Map<ConversationDTO>(conv);
        }


        // ========================================================
        // 2. START Chat with AI
        // ========================================================
        public async Task<ConversationDTO> StartAIConversation(string buyerId, string productId)
        {
            var existing = await _uow.Conversations.GetByKeyAsync(
                buyerId,
                sellerId: null,
                productId,
                isAI: true
            );

            if (existing != null)
                return _mapper.Map<ConversationDTO>(existing);

            var conv = new Conversation
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                SellerId = null,
                ProductId = productId,
                IsAIChat = true,
                LastMessage = "",
                LastMessageAt = DateTime.UtcNow
            };

            await _uow.Conversations.AddAsync(conv);
            await _uow.CompleteAsync();

            var loaded = await _uow.Conversations.GetAsync(
                c => c.Id == conv.Id,
                includeProperties: "Product,Product.Images,Product.Shop"
            );

            return _mapper.Map<ConversationDTO>(loaded);
        }


        // ========================================================
        // 3. SEND message to seller
        // ========================================================
        public async Task<MessageDTO> SendSellerMessage(Guid conversationId, string senderId, string content)
        {
            var conversation = await _uow.Conversations.GetAsync(
                c => c.Id == conversationId,
                includeProperties: "Messages"
            );

            if (conversation == null)
                throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện.");

            if (conversation.IsAIChat)
                throw new InvalidOperationException("Cuộc trò chuyện này là trò chuyện AI.");

            if (conversation.BuyerId != senderId && conversation.SellerId != senderId)
                throw new UnauthorizedAccessException("Bạn không phải là một phần của cuộc trò chuyện này.");

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Messages.AddAsync(message);

            conversation.LastMessage = content;
            conversation.LastMessageAt = DateTime.UtcNow;

            await _uow.CompleteAsync();

            return await MapMessageAsync(message);
        }


        // ========================================================
        // 4. SEND message to AI
        // ========================================================
        public async Task<MessageDTO> SendAIMessage(Guid conversationId, string senderId, string content)
        {
            var conversation = await _uow.Conversations.GetAsync(
                c => c.Id == conversationId,
                includeProperties: "Product,Product.Shop,Product.Images"
            );

            if (conversation == null)
                throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện.");

            if (!conversation.IsAIChat)
                throw new InvalidOperationException("Đây không phải là cuộc trò chuyện AI.");

            var userMessage = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Messages.AddAsync(userMessage);

            var aiResponse = await _ai.GetProductAnswerAsync(conversation.Product, content);

            var aiMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = "AI_SYSTEM",
                Content = aiResponse,
                IsRead = true,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Messages.AddAsync(aiMsg);

            conversation.LastMessage = aiResponse;
            conversation.LastMessageAt = DateTime.UtcNow;

            await _uow.CompleteAsync();

            return await MapMessageAsync(aiMsg);
        }


        // ========================================================
        // 5. GET ALL messages + mark IsRead
        // ========================================================
        public async Task<IEnumerable<MessageDTO>> GetMessages(Guid conversationId, string currentUserId)
        {
            var conv = await _uow.Conversations.GetAsync(c => c.Id == conversationId);

            if (conv == null)
                throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện");

            var messages = await _uow.Messages.GetByConversationAsync(conversationId);

            var unread = messages
                .Where(m => !m.IsRead && m.SenderId != currentUserId)
                .ToList();

            if (unread.Any())
            {
                foreach (var m in unread)
                    m.IsRead = true;

                await _uow.CompleteAsync();
            }

            return await MapMessagesAsync(messages);
        }


        // ========================================================
        // 6. GET conversations (BUYER)
        // ========================================================
        public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId)
        {
            return await _uow.Conversations.GetAllAsync(
                c => c.BuyerId == userId,
                includeProperties: "Product,Product.Images,Product.Shop,Buyer,Seller,Seller.Shop"
            );
        }


        // ========================================================
        // 7. GET conversations (SELLER)
        // ========================================================
        public async Task<IEnumerable<Conversation>> GetSellerConversationsAsync(string sellerId)
        {
            return await _uow.Conversations.GetAllAsync(
                c => c.SellerId == sellerId && c.IsAIChat == false,
                includeProperties: "Product,Product.Images,Product.Shop,Buyer,Seller,Seller.Shop"
            );
        }


        // ========================================================
        // Helper: Map Message + include Sender info
        // ========================================================
        private async Task<MessageDTO> MapMessageAsync(Message msg)
        {
            var dto = _mapper.Map<MessageDTO>(msg);

            if (msg.SenderId == "AI_SYSTEM")
            {
                dto.SenderName = "AI Assistant";
                dto.SenderAvatar = "https://i.ibb.co/8mCVxNd/ai-avatar.png";
            }
            else
            {
                var user = await _uow.Users.GetAsync(u => u.Id == msg.SenderId);
                dto.SenderName = user?.FullName;
                dto.SenderAvatar = user?.ImageUrl;
            }

            return dto;
        }


        private async Task<IEnumerable<MessageDTO>> MapMessagesAsync(IEnumerable<Message> msgs)
        {
            var list = new List<MessageDTO>();

            foreach (var msg in msgs)
                list.Add(await MapMessageAsync(msg));

            return list;
        }
        public async Task<ConversationDTO> GetUserConversationById(Guid conversationId, string userId)
        {
            var conv = await _uow.Conversations.GetAsync(
                c => c.Id == conversationId && c.BuyerId == userId,
                includeProperties: "Product,Product.Images,Product.Shop,Buyer,Seller,Seller.Shop"
            );

            if (conv == null)
                throw new UnauthorizedAccessException("Bạn không sở hữu cuộc trò chuyện này.");

            return _mapper.Map<ConversationDTO>(conv);
        }

        public async Task<ConversationDTO> GetSellerConversationById(Guid conversationId, string sellerId)
        {
            var conv = await _uow.Conversations.GetAsync(
                c => c.Id == conversationId && c.SellerId == sellerId && c.IsAIChat == false,
                includeProperties: "Product,Product.Images,Product.Shop,Buyer,Seller,Seller.Shop"

            );

            if (conv == null)
                throw new UnauthorizedAccessException("Bạn không sở hữu cuộc trò chuyện này.");

            return _mapper.Map<ConversationDTO>(conv);
        }
        public async Task<MessageDTO> SendAIUserMessage(Guid conversationId, string senderId, string content)
        {
            var conversation = await _uow.Conversations.GetAsync(
                c => c.Id == conversationId,
                includeProperties: "Product,Product.Shop,Product.Images"
            );

            if (conversation == null)
                throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện.");

            if (!conversation.IsAIChat)
                throw new InvalidOperationException("Đây không phải là cuộc trò chuyện AI.");

            var userMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Messages.AddAsync(userMsg);

            conversation.LastMessage = content;
            conversation.LastMessageAt = DateTime.UtcNow;

            await _uow.CompleteAsync();

            return await MapMessageAsync(userMsg);
        }
        public async Task<MessageDTO> SendAIReply(Guid conversationId, string senderId, string content)
        {
            var conversation = await _uow.Conversations.GetAsync(
                c => c.Id == conversationId,
                includeProperties: "Product,Product.Shop,Product.Images"
            );

            if (conversation == null)
                throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện.");

            if (!conversation.IsAIChat)
                throw new InvalidOperationException("Đây không phải là cuộc trò chuyện AI.");

            var aiResponse = await _ai.GetProductAnswerAsync(conversation.Product, content);

            var aiMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = "AI_SYSTEM",
                Content = aiResponse,
                IsRead = true,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Messages.AddAsync(aiMsg);

            conversation.LastMessage = aiResponse;
            conversation.LastMessageAt = DateTime.UtcNow;

            await _uow.CompleteAsync();

            return await MapMessageAsync(aiMsg);
        }

    }
}
