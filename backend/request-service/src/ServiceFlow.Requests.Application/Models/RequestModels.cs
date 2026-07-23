using ServiceFlow.Requests.Domain.Enums;

namespace ServiceFlow.Requests.Application.Models;

public sealed record CreateRequestCommand(
    string Title,
    string Description,
    RequestCategory Category,
    RequestPriority Priority);

public sealed record UpdateRequestCommand(
    string Title,
    string Description,
    RequestCategory Category,
    RequestPriority Priority);

public sealed record ChangeRequestStatusCommand(RequestStatus Status);
public sealed record AssignRequestCommand(string? AssignedTo);
public sealed record AddCommentCommand(string Content);

public sealed record RequestCommentDto(
    long Id,
    long RequestId,
    string AuthorId,
    string Content,
    DateTimeOffset CreatedAt);

public sealed record RequestHistoryDto(
    long Id,
    long RequestId,
    RequestStatus PreviousStatus,
    RequestStatus NewStatus,
    string ChangedBy,
    DateTimeOffset ChangedAt);

public sealed record RequestDto(
    long Id,
    string Title,
    string Description,
    RequestCategory Category,
    RequestPriority Priority,
    RequestStatus Status,
    string CreatedBy,
    string? AssignedTo,
    DateTimeOffset DueAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<RequestCommentDto> Comments);

public sealed class RequestFilter
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public RequestStatus? Status { get; init; }
    public RequestPriority? Priority { get; init; }
    public RequestCategory? Category { get; init; }
    public string? AssignedTo { get; init; }
    public string? CreatedBy { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public string SortDirection { get; init; } = "desc";
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, long Total, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
