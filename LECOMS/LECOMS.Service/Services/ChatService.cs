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
            // Lấy product + seller
            var product = await _uow.Products.GetAsync(
                p => p.Id == productId,
                includeProperties: "Shop,Images,Category");

            if (product == null)
                throw new KeyNotFoundException("Product not found.");

            var sellerId = product.Shop.SellerId;

            if (buyerId == sellerId)
                throw new InvalidOperationException("Seller cannot chat with themselves.");

            // Check nếu đã tồn tại cuộc trò chuyện
            var existing = await _uow.Conversations.GetByKeyAsync(
                buyerId,
                sellerId,
                productId,
                isAI: false
            );

            if (existing != null)
                return _mapper.Map<ConversationDTO>(existing);

            // Tạo mới conversation
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
        // 3. SEND MESSAGE TO SELLER
        // ========================================================
        public async Task<MessageDTO> SendSellerMessage(Guid conversationId, string senderId, string content)
        {
            var conversation = await _uow.Conversations.GetAsync(
                c => c.Id == conversationId,
                includeProperties: "Messages"
            );

            if (conversation == null)
                throw new KeyNotFoundException("Conversation not found.");

            if (conversation.IsAIChat)
                throw new InvalidOperationException("This conversation is AI chat.");

            if (conversation.BuyerId != senderId && conversation.SellerId != senderId)
                throw new UnauthorizedAccessException("You are not part of this conversation.");

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

            var dto = _mapper.Map<MessageDTO>(message);
            dto.IsMe = message.SenderId == senderId;

            return dto;
        }

        // ========================================================
        // 4. SEND MESSAGE TO AI
        // ========================================================
        public async Task<MessageDTO> SendAIMessage(Guid conversationId, string senderId, string content)
        {
            var conversation = await _uow.Conversations.GetAsync(
                c => c.Id == conversationId,
                includeProperties: "Product,Product.Shop,Product.Images"
            );

            if (conversation == null)
                throw new KeyNotFoundException("Conversation not found.");

            if (!conversation.IsAIChat)
                throw new InvalidOperationException("This is not an AI chat.");

            // Save user message
            var userMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Messages.AddAsync(userMsg);

            // AI response
            var aiResponseText =
                await _ai.GetProductAnswerAsync(conversation.Product, content);

            var aiMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = "AI_SYSTEM",
                Content = aiResponseText,
                IsRead = true,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Messages.AddAsync(aiMsg);

            conversation.LastMessage = aiResponseText;
            conversation.LastMessageAt = DateTime.UtcNow;

            await _uow.CompleteAsync();

            var dto = _mapper.Map<MessageDTO>(aiMsg);
            dto.IsMe = false;

            return dto;
        }

        // ========================================================
        // 5. GET ALL MESSAGES
        // ========================================================
        public async Task<IEnumerable<MessageDTO>> GetMessages(Guid conversationId)
        {
            var conv = await _uow.Conversations.GetAsync(
                c => c.Id == conversationId
            );

            if (conv == null)
                throw new KeyNotFoundException("Conversation not found.");

            var messages = await _uow.Messages.GetByConversationAsync(conversationId);


            var dtoList = _mapper.Map<IEnumerable<MessageDTO>>(messages);

            foreach (var dto in dtoList)
            {
                dto.IsMe = dto.SenderId == conv.BuyerId || dto.SenderId == conv.SellerId;
            }

            return dtoList;
        }

        // ========================================================
        // 6. GET USER CONVERSATIONS (BUYER ONLY)
        // ========================================================
        public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId)
        {
            return await _uow.Conversations.GetAllAsync(
                c => c.BuyerId == userId,
                includeProperties: "Product,Buyer,Seller"
            );
        }

        // ========================================================
        // 7. GET SELLER CONVERSATIONS
        // ========================================================
        public async Task<IEnumerable<Conversation>> GetSellerConversationsAsync(string sellerId)
        {
            return await _uow.Conversations.GetAllAsync(
                c => c.SellerId == sellerId && c.IsAIChat == false,
                includeProperties: "Product,Buyer,Seller"
            );
        }
    }
}
