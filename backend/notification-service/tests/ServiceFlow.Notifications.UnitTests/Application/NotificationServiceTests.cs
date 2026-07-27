using ServiceFlow.Notifications.Application.Abstractions;
using ServiceFlow.Notifications.Application.Services;
using ServiceFlow.Notifications.Domain.Entities;

namespace ServiceFlow.Notifications.UnitTests.Application;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task SearchAsync_ReturnsExpectedPageContract()
    {
        var notifications = new[]
        {
            CreateNotification("RequestCreated"),
            CreateNotification("CommentAdded")
        };
        var repository = new FakeNotificationRepository(notifications, total: 7);
        var service = new NotificationService(repository);

        var result = await service.SearchAsync(
            "employee@serviceflow.local",
            isRead: false,
            page: 2,
            pageSize: 2,
            CancellationToken.None);

        Assert.Equal(7, result.Total);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(4, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(false, repository.LastIsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_PersistsOnlyTheFirstTransition()
    {
        var notification = CreateNotification("RequestAssigned");
        var repository = new FakeNotificationRepository([notification]);
        var service = new NotificationService(repository);

        var first = await service.MarkAsReadAsync(
            notification.Id,
            notification.UserId,
            CancellationToken.None);
        var second = await service.MarkAsReadAsync(
            notification.Id,
            notification.UserId,
            CancellationToken.None);

        Assert.NotNull(first);
        Assert.True(first.IsRead);
        Assert.NotNull(second);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task MarkAsReadAsync_DoesNotExposeAnotherUsersNotification()
    {
        var notification = CreateNotification("RequestUpdated");
        var repository = new FakeNotificationRepository([notification]);
        var service = new NotificationService(repository);

        var result = await service.MarkAsReadAsync(
            notification.Id,
            "another-user@serviceflow.local",
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, repository.SaveChangesCalls);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task SearchAsync_RejectsInvalidPagination(int page, int pageSize)
    {
        var repository = new FakeNotificationRepository([]);
        var service = new NotificationService(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.SearchAsync(
            "employee@serviceflow.local",
            null,
            page,
            pageSize,
            CancellationToken.None));

        Assert.Equal(0, repository.SearchCalls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Operations_RejectMissingUserId(string userId)
    {
        var repository = new FakeNotificationRepository([]);
        var service = new NotificationService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CountUnreadAsync(userId, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.MarkAllAsReadAsync(userId, CancellationToken.None));
    }

    [Fact]
    public async Task CountAndMarkAll_ForwardRepositoryResults()
    {
        var notifications = new[] { CreateNotification("RequestCreated"), CreateNotification("CommentAdded") };
        var repository = new FakeNotificationRepository(notifications) { MarkAllResult = 2 };
        var service = new NotificationService(repository);

        var unread = await service.CountUnreadAsync("employee@serviceflow.local", CancellationToken.None);
        var marked = await service.MarkAllAsReadAsync("employee@serviceflow.local", CancellationToken.None);

        Assert.Equal(2, unread);
        Assert.Equal(2, marked);
        Assert.Equal("employee@serviceflow.local", repository.LastUserId);
    }

    private static Notification CreateNotification(string type) => Notification.Create(
        "employee@serviceflow.local",
        type,
        "Notification title",
        "Notification message",
        Guid.NewGuid());

    private sealed class FakeNotificationRepository(
        IReadOnlyCollection<Notification> notifications,
        long? total = null) : INotificationRepository
    {
        public bool? LastIsRead { get; private set; }
        public int SaveChangesCalls { get; private set; }
        public int SearchCalls { get; private set; }
        public string? LastUserId { get; private set; }
        public int MarkAllResult { get; init; }

        public Task<(IReadOnlyCollection<Notification> Items, long Total)> SearchAsync(
            string userId,
            bool? isRead,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            SearchCalls++;
            LastUserId = userId;
            LastIsRead = isRead;
            return Task.FromResult((notifications, total ?? notifications.Count));
        }

        public Task<long> CountUnreadAsync(
            string userId,
            CancellationToken cancellationToken)
        {
            LastUserId = userId;
            return Task.FromResult((long)notifications.Count(notification => !notification.IsRead));
        }

        public Task<Notification?> GetAsync(
            Guid id,
            string userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(notifications.SingleOrDefault(notification =>
                notification.Id == id && notification.UserId == userId));

        public Task<int> MarkAllAsReadAsync(
            string userId,
            CancellationToken cancellationToken)
        {
            LastUserId = userId;
            return Task.FromResult(MarkAllResult);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }
}
