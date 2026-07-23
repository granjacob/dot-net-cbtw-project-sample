using ServiceFlow.Notifications.Application.Abstractions;
using ServiceFlow.Notifications.Application.Contracts;

namespace ServiceFlow.Notifications.Application.Services;

public sealed class NotificationService(INotificationRepository repository)
{
    public async Task<PagedResult<NotificationDto>> SearchAsync(
        string userId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100);

        var result = await repository.SearchAsync(
            userId,
            isRead,
            page,
            pageSize,
            cancellationToken);

        return new PagedResult<NotificationDto>(
            result.Items.Select(NotificationDto.FromEntity).ToArray(),
            result.Total,
            page,
            pageSize);
    }

    public Task<PagedResult<NotificationDto>> GetUnreadAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        SearchAsync(userId, false, page, pageSize, cancellationToken);

    public Task<long> CountUnreadAsync(string userId, CancellationToken cancellationToken)
    {
        ValidateUserId(userId);
        return repository.CountUnreadAsync(userId, cancellationToken);
    }

    public async Task<NotificationDto?> MarkAsReadAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);

        var notification = await repository.GetAsync(id, userId, cancellationToken);
        if (notification is null)
        {
            return null;
        }

        if (notification.MarkAsRead())
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return NotificationDto.FromEntity(notification);
    }

    public Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken)
    {
        ValidateUserId(userId);
        return repository.MarkAllAsReadAsync(userId, cancellationToken);
    }

    private static void ValidateUserId(string userId) =>
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
}
