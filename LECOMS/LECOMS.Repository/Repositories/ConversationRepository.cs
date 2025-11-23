using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class ConversationRepository : Repository<Conversation>, IConversationRepository
    {
        private readonly LecomDbContext _db;

        public ConversationRepository(LecomDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<Conversation> GetByKeyAsync(string buyerId, string? sellerId, string productId, bool isAI)
        {
            return await _db.Conversations
                .Include(c => c.Product)
                    .ThenInclude(p => p.Images)
                .Include(c => c.Product.Shop)
                .Include(c => c.Buyer)
                .Include(c => c.Seller)
                .FirstOrDefaultAsync(c =>
                    c.BuyerId == buyerId &&
                    c.ProductId == productId &&
                    c.IsAIChat == isAI &&
                    (c.SellerId == sellerId || sellerId == null)
                );
        }
    }

}
