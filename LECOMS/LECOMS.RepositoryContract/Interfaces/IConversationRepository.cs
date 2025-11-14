using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface IConversationRepository : IRepository<Conversation>
    {
        Task<Conversation> GetByKeyAsync(string buyerId, string? sellerId, string productId, bool isAI);
    }

}
