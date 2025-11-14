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
    public class MessageRepository : Repository<Message>, IMessageRepository
    {
        private readonly LecomDbContext _db;

        public MessageRepository(LecomDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Message>> GetByConversationAsync(Guid conversationId)
        {
            return await _db.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
    }

}
