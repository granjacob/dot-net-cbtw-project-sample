using ServiceFlow.Notifications.Domain.Entities;

namespace ServiceFlow.Notifications.UnitTests.Domain;

public sealed class NotificationTests
{
    [Fact]
    public void Create_InitializesUnreadNotification()
    {
        var eventId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-07-22T12:00:00Z");

        var notification = Notification.Create(
            "employee@serviceflow.local",
            "RequestCreated",
            "Solicitud creada",
            "La solicitud fue creada.",
            eventId,
            createdAt,
            requestId: 148);

        Assert.NotEqual(Guid.Empty, notification.Id);
        Assert.Equal(eventId, notification.EventId);
        Assert.Equal(createdAt, notification.CreatedAt);
        Assert.Equal(148, notification.RequestId);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void MarkAsRead_IsIdempotent()
    {
        var notification = CreateNotification();

        Assert.True(notification.MarkAsRead());
        Assert.True(notification.IsRead);
        Assert.False(notification.MarkAsRead());
    }

    [Fact]
    public void Create_RejectsEmptyEventId()
    {
        Assert.Throws<ArgumentException>(() => Notification.Create(
            "employee@serviceflow.local",
            "RequestCreated",
            "Solicitud creada",
            "La solicitud fue creada.",
            Guid.Empty));
    }

    private static Notification CreateNotification() => Notification.Create(
        "employee@serviceflow.local",
        "RequestUpdated",
        "Solicitud actualizada",
        "La solicitud fue actualizada.",
        Guid.NewGuid());
}
