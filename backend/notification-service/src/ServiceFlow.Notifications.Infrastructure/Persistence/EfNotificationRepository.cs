using Microsoft.EntityFrameworkCore;
using ServiceFlow.Notifications.Application.Abstractions;
using ServiceFlow.Notifications.Domain.Entities;

namespace ServiceFlow.Notifications.Infrastructure.Persistence;

internal sealed class EfNotificationRepository(NotificationDbContext dbContext)
    : INotificationRepository
{
    public async Task<(IReadOnlyCollection<Notification> Items, long Total)> SearchAsync(
        string userId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId);

        if (isRead.HasValue)
        {
            query = query.Where(notification => notification.IsRead == isRead.Value);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return (items, total);
    }

    public Task<long> CountUnreadAsync(string userId, CancellationToken cancellationToken) =>
        dbContext.Notifications.LongCountAsync(
            notification => notification.UserId == userId && !notification.IsRead,
            cancellationToken);

    public Task<Notification?> GetAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken) =>
        dbContext.Notifications.SingleOrDefaultAsync(
            notification => notification.Id == id && notification.UserId == userId,
            cancellationToken);

    public Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken) =>
        dbContext.Notifications
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(notification => notification.IsRead, true),
                cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
