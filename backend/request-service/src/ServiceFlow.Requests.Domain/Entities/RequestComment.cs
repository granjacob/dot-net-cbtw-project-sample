namespace ServiceFlow.Requests.Domain.Entities;

public sealed class RequestComment
{
    private RequestComment()
    {
    }

    public long Id { get; private set; }
    public long RequestId { get; private set; }
    public string AuthorId { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    internal static RequestComment Create(long requestId, string authorId, string content, DateTimeOffset createdAt) =>
        new()
        {
            RequestId = requestId,
            AuthorId = authorId.Trim(),
            Content = content.Trim(),
            CreatedAt = createdAt.ToUniversalTime()
        };
}
