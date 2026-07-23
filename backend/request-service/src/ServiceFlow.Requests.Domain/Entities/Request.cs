using ServiceFlow.Requests.Domain.Enums;

namespace ServiceFlow.Requests.Domain.Entities;

public sealed class Request
{
    private readonly List<RequestComment> _comments = [];
    private readonly List<RequestHistory> _history = [];

    private Request()
    {
    }

    public long Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public RequestCategory Category { get; private set; }
    public RequestPriority Priority { get; private set; }
    public RequestStatus Status { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public string? AssignedTo { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset DueAt { get; private set; }
    public IReadOnlyCollection<RequestComment> Comments => _comments.AsReadOnly();
    public IReadOnlyCollection<RequestHistory> History => _history.AsReadOnly();

    public static Request Create(
        long id,
        string title,
        string description,
        RequestCategory category,
        RequestPriority priority,
        string createdBy,
        DateTimeOffset createdAt,
        DateTimeOffset dueAt)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        ValidateDetails(title, description, category, priority);
        ValidateActor(createdBy, nameof(createdBy));

        var timestamp = createdAt.ToUniversalTime();
        var request = new Request
        {
            Id = id,
            Title = title.Trim(),
            Description = description.Trim(),
            Category = category,
            Priority = priority,
            Status = RequestStatus.Open,
            CreatedBy = createdBy.Trim(),
            CreatedAt = timestamp,
            UpdatedAt = timestamp,
            DueAt = dueAt.ToUniversalTime()
        };
        request._history.Add(RequestHistory.Create(id, RequestStatus.Open, RequestStatus.Open, createdBy, timestamp));
        return request;
    }

    public void Update(
        string title,
        string description,
        RequestCategory category,
        RequestPriority priority,
        DateTimeOffset updatedAt,
        DateTimeOffset dueAt)
    {
        ValidateDetails(title, description, category, priority);
        EnsureNotClosed();

        Title = title.Trim();
        Description = description.Trim();
        Category = category;
        Priority = priority;
        DueAt = dueAt.ToUniversalTime();
        UpdatedAt = updatedAt.ToUniversalTime();
    }

    public void ChangeStatus(RequestStatus newStatus, string changedBy, DateTimeOffset changedAt)
    {
        ValidateActor(changedBy, nameof(changedBy));
        if (!Enum.IsDefined(newStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(newStatus));
        }

        if (newStatus == Status)
        {
            throw new InvalidOperationException($"Request is already {newStatus}.");
        }

        if (!CanTransition(Status, newStatus))
        {
            throw new InvalidOperationException($"Cannot transition a request from {Status} to {newStatus}.");
        }

        var previous = Status;
        var timestamp = changedAt.ToUniversalTime();
        Status = newStatus;
        UpdatedAt = timestamp;
        _history.Add(RequestHistory.Create(Id, previous, newStatus, changedBy, timestamp));
    }

    public void Assign(string? assignedTo, DateTimeOffset updatedAt)
    {
        EnsureNotClosed();
        if (assignedTo is not null)
        {
            ValidateActor(assignedTo, nameof(assignedTo));
        }

        AssignedTo = string.IsNullOrWhiteSpace(assignedTo) ? null : assignedTo.Trim();
        UpdatedAt = updatedAt.ToUniversalTime();
    }

    public RequestComment AddComment(string authorId, string content, DateTimeOffset createdAt)
    {
        ValidateActor(authorId, nameof(authorId));
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (content.Trim().Length > 2_000)
        {
            throw new ArgumentException("Comment content cannot exceed 2000 characters.", nameof(content));
        }

        var timestamp = createdAt.ToUniversalTime();
        var comment = RequestComment.Create(Id, authorId, content, timestamp);
        _comments.Add(comment);
        UpdatedAt = timestamp;
        return comment;
    }

    private static bool CanTransition(RequestStatus current, RequestStatus next) => (current, next) switch
    {
        (RequestStatus.Open, RequestStatus.Pending or RequestStatus.InProgress or RequestStatus.Closed) => true,
        (RequestStatus.Pending, RequestStatus.Open or RequestStatus.InProgress or RequestStatus.Closed) => true,
        (RequestStatus.InProgress, RequestStatus.Pending or RequestStatus.Resolved or RequestStatus.Closed) => true,
        (RequestStatus.Resolved, RequestStatus.InProgress or RequestStatus.Closed) => true,
        _ => false
    };

    private void EnsureNotClosed()
    {
        if (Status == RequestStatus.Closed)
        {
            throw new InvalidOperationException("Closed requests cannot be modified.");
        }
    }

    private static void ValidateDetails(
        string title,
        string description,
        RequestCategory category,
        RequestPriority priority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        var normalizedTitle = title.Trim();
        var normalizedDescription = description.Trim();
        if (normalizedTitle.Length is < 5 or > 160)
        {
            throw new ArgumentException("Title must contain between 5 and 160 characters.", nameof(title));
        }

        if (normalizedDescription.Length is < 20 or > 4_000)
        {
            throw new ArgumentException("Description must contain between 20 and 4000 characters.", nameof(description));
        }

        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }

        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }
    }

    private static void ValidateActor(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Trim().Length > 256)
        {
            throw new ArgumentException("User identifier cannot exceed 256 characters.", parameterName);
        }
    }
}
