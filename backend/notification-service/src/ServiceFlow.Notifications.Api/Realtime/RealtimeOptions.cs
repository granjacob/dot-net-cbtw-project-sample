namespace ServiceFlow.Notifications.Api.Realtime;

public sealed class RealtimeOptions
{
    public const string SectionName = "SignalR";

    public bool BroadcastToAll { get; init; } = false;
}
