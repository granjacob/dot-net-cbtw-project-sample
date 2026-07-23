using System.Text.Json;
using ServiceFlow.Notifications.Application.Contracts;
using ServiceFlow.Notifications.Infrastructure.Messaging;

namespace ServiceFlow.Notifications.UnitTests.Infrastructure;

public sealed class NotificationEventFactoryTests
{
    [Fact]
    public void Create_UsesDataFallbackAndCarriesRequestId()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "userId": "employee@serviceflow.local",
              "title": "Solicitud asignada",
              "message": "La solicitud fue asignada."
            }
            """);
        var integrationEvent = new RequestEventEnvelope
        {
            EventId = Guid.NewGuid(),
            EventType = "RequestAssigned",
            OccurredAt = DateTimeOffset.UtcNow,
            RequestId = 148,
            Data = document.RootElement.Clone()
        };

        var notification = NotificationEventFactory.Create(integrationEvent);

        Assert.Equal("employee@serviceflow.local", notification.UserId);
        Assert.Equal("Solicitud asignada", notification.Title);
        Assert.Equal("La solicitud fue asignada.", notification.Message);
        Assert.Equal(148, notification.RequestId);
    }
}
