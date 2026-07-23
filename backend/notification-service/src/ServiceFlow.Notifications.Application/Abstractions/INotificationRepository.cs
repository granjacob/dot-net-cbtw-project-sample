using ServiceFlow.Notifications.Domain.Entities;

namespace ServiceFlow.Notifications.Application.Abstractions;

public interface INotificationRepository
{
    Task<(IReadOnlyCollection<Notification> Items, long Total)> SearchAsync(
        string userId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<long> CountUnreadAsync(string userId, CancellationToken cancellationToken);

    Task<Notification?> GetAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken);

    Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
