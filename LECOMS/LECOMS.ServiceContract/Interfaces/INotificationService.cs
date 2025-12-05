using LECOMS.Data.DTOs.Notification;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDTO> CreateAsync(string userId, string type, string title, string? content = null);
        Task<IEnumerable<NotificationDTO>> GetForUserAsync(string userId, int page, int size);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(string id, string userId);
        Task MarkAllAsReadAsync(string userId);
    }
}
