using ServiceFlow.Requests.Domain.Enums;

namespace ServiceFlow.Requests.Domain.Entities;

public sealed class RequestHistory
{
    private RequestHistory()
    {
    }

    public long Id { get; private set; }
    public long RequestId { get; private set; }
    public RequestStatus PreviousStatus { get; private set; }
    public RequestStatus NewStatus { get; private set; }
    public string ChangedBy { get; private set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; private set; }

    internal static RequestHistory Create(
        long requestId,
        RequestStatus previousStatus,
        RequestStatus newStatus,
        string changedBy,
        DateTimeOffset changedAt) =>
        new()
        {
            RequestId = requestId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedBy = changedBy.Trim(),
            ChangedAt = changedAt.ToUniversalTime()
        };
}
