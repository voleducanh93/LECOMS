using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LECOMS.Repository.Repositories
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        public NotificationRepository(LecomDbContext db) : base(db) { }

        public async Task<IEnumerable<Notification>> GetByUserAsync(string userId, int page, int size)
        {
            return await dbSet
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await dbSet
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }
    }
}
