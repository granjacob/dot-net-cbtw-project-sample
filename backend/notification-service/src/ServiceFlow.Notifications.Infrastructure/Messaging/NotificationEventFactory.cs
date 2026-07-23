using System.Text.Json;
using ServiceFlow.Notifications.Application.Contracts;
using ServiceFlow.Notifications.Domain.Entities;

namespace ServiceFlow.Notifications.Infrastructure.Messaging;

internal static class NotificationEventFactory
{
    private const string DefaultUserId = "employee@serviceflow.local";

    public static Notification Create(RequestEventEnvelope integrationEvent)
    {
        var userId = FirstNotEmpty(
            integrationEvent.UserId,
            GetDataString(integrationEvent.Data, "userId"),
            GetDataString(integrationEvent.Data, "createdBy"),
            DefaultUserId)!;

        var title = FirstNotEmpty(
            integrationEvent.Title,
            GetDataString(integrationEvent.Data, "title"),
            GetDefaultTitle(integrationEvent.EventType))!;

        var message = FirstNotEmpty(
            integrationEvent.Message,
            GetDataString(integrationEvent.Data, "message"),
            $"La solicitud #{integrationEvent.RequestId} generó el evento {integrationEvent.EventType}.")!;

        return Notification.Create(
            Limit(userId, 256),
            Limit(integrationEvent.EventType, 100),
            Limit(title, 200),
            Limit(message, 2000),
            integrationEvent.EventId,
            requestId: integrationEvent.RequestId > 0 ? integrationEvent.RequestId : null);
    }

    private static string GetDefaultTitle(string eventType) => eventType switch
    {
        "RequestCreated" => "Solicitud creada",
        "RequestUpdated" => "Solicitud actualizada",
        "RequestAssigned" => "Solicitud asignada",
        "RequestStatusChanged" => "Estado de solicitud actualizado",
        "CommentAdded" => "Nuevo comentario",
        _ => "Actualización de solicitud"
    };

    private static string? GetDataString(JsonElement? data, string propertyName)
    {
        if (!data.HasValue || data.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in data.Value.EnumerateObject())
        {
            if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.Value.ToString(),
                _ => null
            };
        }

        return null;
    }

    private static string? FirstNotEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string Limit(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
