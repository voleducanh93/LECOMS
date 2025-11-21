using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
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
    public class CommunityPostRepository : Repository<CommunityPost>, ICommunityPostRepository
    {
        public CommunityPostRepository(LecomDbContext db) : base(db) { }

        public async Task<IEnumerable<CommunityPost>> GetPendingAsync()
        {
            return await dbSet.Where(p => p.ApprovalStatus == ApprovalStatus.Pending)
                              .Include(p => p.User)
                              .ToListAsync();
        }
    }

}
