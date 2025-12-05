using LECOMS.Common.Hubs;
using LECOMS.Data.DTOs.Notification;
using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LECOMS.Service.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(IUnitOfWork uow, IHubContext<NotificationHub> hub)
        {
            _uow = uow;
            _hub = hub;
        }

        public async Task<NotificationDTO> CreateAsync(
            string userId,
            string type,
            string title,
            string? content = null)
        {
            var entity = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = type,
                Title = title,
                Content = content,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Notifications.AddAsync(entity);
            await _uow.CompleteAsync();

            var dto = new NotificationDTO
            {
                Id = entity.Id,
                Type = entity.Type,
                Title = entity.Title,
                Content = entity.Content,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt
            };

            // Realtime
            await _hub.Clients.Group(userId)
                .SendAsync("ReceiveNotification", dto);

            return dto;
        }

        public async Task<IEnumerable<NotificationDTO>> GetForUserAsync(string userId, int page, int size)
        {
            var list = await _uow.Notifications.GetByUserAsync(userId, page, size);
            return list.Select(n => new NotificationDTO
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Content = n.Content,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });
        }

        public Task<int> GetUnreadCountAsync(string userId)
        {
            return _uow.Notifications.GetUnreadCountAsync(userId);
        }

        public async Task MarkAsReadAsync(string id, string userId)
        {
            var item = await _uow.Notifications.GetAsync(n => n.Id == id && n.UserId == userId);
            if (item == null) return;

            item.IsRead = true;
            await _uow.Notifications.UpdateAsync(item);
            await _uow.CompleteAsync();
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var list = await _uow.Notifications.GetByUserAsync(userId, 1, int.MaxValue);
            foreach (var n in list.Where(x => !x.IsRead))
            {
                n.IsRead = true;
                await _uow.Notifications.UpdateAsync(n);
            }
            await _uow.CompleteAsync();
        }
        // ⭐⭐ MỚI: Broadcast cho toàn bộ user đang active
        public async Task<int> BroadcastToAllUsersAsync(string type, string title, string? content = null)
        {
            // Lấy tất cả user đang active
            var users = await _uow.Users.GetAllUsersAsync(); // ⭐ dùng hàm có sẵn
            var activeUsers = users.Where(u => u.IsActive).ToList();

            int count = 0;

            foreach (var user in users)
            {
                await CreateAsync(user.Id, type, title, content);
                count++;
            }

            return count;
        }
    }
}
