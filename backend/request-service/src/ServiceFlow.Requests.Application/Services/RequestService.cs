using System.Text.Json;
using System.Text.Json.Serialization;
using ServiceFlow.Requests.Application.Abstractions;
using ServiceFlow.Requests.Application.Common;
using ServiceFlow.Requests.Application.Models;
using ServiceFlow.Requests.Application.Sla;
using ServiceFlow.Requests.Domain.Entities;

namespace ServiceFlow.Requests.Application.Services;

public sealed class RequestService(
    IRequestRepository requests,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork,
    ISlaStrategyFactory slaFactory,
    IClock clock,
    ICurrentUser currentUser,
    ICorrelationIdAccessor correlation,
    IRequestIdGenerator idGenerator) : IRequestService
{
    private static readonly JsonSerializerOptions EventJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<Result<RequestDto>> CreateAsync(
        CreateRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = EnsureCurrentUser();
        if (authorization is not null)
        {
            return Result<RequestDto>.Failure(authorization);
        }

        var validation = ValidateDetails(command.Title, command.Description, command.Category, command.Priority);
        if (validation is not null)
        {
            return Result<RequestDto>.Failure(validation);
        }

        var now = clock.UtcNow.ToUniversalTime();
        var dueAt = slaFactory.Create(command.Priority).CalculateDueDate(now).ToUniversalTime();
        var request = Request.Create(
            idGenerator.NewId(),
            command.Title,
            command.Description,
            command.Category,
            command.Priority,
            currentUser.UserId,
            now,
            dueAt);

        await requests.AddAsync(request, cancellationToken);
        await AddEventAsync(
            "RequestCreated",
            request,
            "Solicitud creada",
            $"Se creó la solicitud «{request.Title}».",
            new
            {
                request.Title,
                request.Description,
                request.Category,
                request.Priority,
                request.Status,
                request.DueAt,
                actorId = currentUser.UserId
            },
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<RequestDto>.Success(Map(request));
    }

    public async Task<Result<RequestDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return Result<RequestDto>.Failure(NotFound(id));
        }

        var access = EnsureCanAccess(request);
        return access is null
            ? Result<RequestDto>.Success(Map(request))
            : Result<RequestDto>.Failure(access);
    }

    public async Task<Result<PagedResult<RequestDto>>> SearchAsync(
        RequestFilter filter,
        CancellationToken cancellationToken = default)
    {
        var authorization = EnsureCurrentUser();
        if (authorization is not null)
        {
            return Result<PagedResult<RequestDto>>.Failure(authorization);
        }

        if (filter.Page < 1)
        {
            return Result<PagedResult<RequestDto>>.Failure(
                Error.Validation("requests.invalid_page", "Page must be greater than zero.", "page", "Page must be greater than zero."));
        }

        if (filter.PageSize is < 1 or > 100)
        {
            return Result<PagedResult<RequestDto>>.Failure(
                Error.Validation("requests.invalid_page_size", "Page size must be between 1 and 100.", "pageSize", "Page size must be between 1 and 100."));
        }

        var allowedSortFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "createdAt", "updatedAt", "dueAt", "title", "priority", "status"
        };
        if (!allowedSortFields.Contains(filter.SortBy))
        {
            return Result<PagedResult<RequestDto>>.Failure(
                Error.Validation("requests.invalid_sort", "The sort field is not supported.", "sortBy", "Use createdAt, updatedAt, dueAt, title, priority or status."));
        }

        if (!string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
        {
            return Result<PagedResult<RequestDto>>.Failure(
                Error.Validation("requests.invalid_sort_direction", "The sort direction is not supported.", "sortDirection", "Use asc or desc."));
        }

        var scopedFilter = IsGlobalReader()
            ? filter
            : new RequestFilter
            {
                Page = filter.Page,
                PageSize = filter.PageSize,
                Search = filter.Search,
                Status = filter.Status,
                Priority = filter.Priority,
                Category = filter.Category,
                AssignedTo = filter.AssignedTo,
                CreatedBy = currentUser.UserId,
                SortBy = filter.SortBy,
                SortDirection = filter.SortDirection
            };
        var result = await requests.SearchAsync(scopedFilter, cancellationToken);
        return Result<PagedResult<RequestDto>>.Success(
            new PagedResult<RequestDto>(result.Items.Select(Map).ToArray(), result.Total, result.Page, result.PageSize));
    }

    public async Task<Result<RequestDto>> UpdateAsync(
        long id,
        UpdateRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateDetails(command.Title, command.Description, command.Category, command.Priority);
        if (validation is not null)
        {
            return Result<RequestDto>.Failure(validation);
        }

        var request = await requests.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return Result<RequestDto>.Failure(NotFound(id));
        }

        var now = clock.UtcNow.ToUniversalTime();
        var previous = new
        {
            request.Title,
            request.Description,
            request.Category,
            request.Priority,
            request.DueAt
        };

        try
        {
            request.Update(
                command.Title,
                command.Description,
                command.Category,
                command.Priority,
                now,
                slaFactory.Create(command.Priority).CalculateDueDate(request.CreatedAt));
        }
        catch (InvalidOperationException exception)
        {
            return Result<RequestDto>.Failure(Error.Conflict("requests.not_editable", exception.Message));
        }

        await AddEventAsync(
            "RequestUpdated",
            request,
            "Solicitud actualizada",
            $"Se actualizó la solicitud «{request.Title}».",
            new
            {
                previous,
                current = new { request.Title, request.Description, request.Category, request.Priority, request.DueAt },
                actorId = currentUser.UserId
            },
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<RequestDto>.Success(Map(request));
    }

    public async Task<Result<RequestDto>> ChangeStatusAsync(
        long id,
        ChangeRequestStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(command.Status))
        {
            return Result<RequestDto>.Failure(
                Error.Validation("requests.invalid_status", "The request status is invalid.", "status", "Use a supported status."));
        }

        var request = await requests.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return Result<RequestDto>.Failure(NotFound(id));
        }

        var previousStatus = request.Status;
        var now = clock.UtcNow.ToUniversalTime();
        try
        {
            request.ChangeStatus(command.Status, currentUser.UserId, now);
        }
        catch (InvalidOperationException exception)
        {
            return Result<RequestDto>.Failure(Error.Conflict("requests.invalid_transition", exception.Message));
        }

        await AddEventAsync(
            "RequestStatusChanged",
            request,
            "Estado actualizado",
            $"La solicitud «{request.Title}» cambió de {previousStatus} a {request.Status}.",
            new { previousStatus, newStatus = request.Status, changedBy = currentUser.UserId },
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<RequestDto>.Success(Map(request));
    }

    public async Task<Result<RequestDto>> AssignAsync(
        long id,
        AssignRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AssignedTo is { Length: > 256 })
        {
            return Result<RequestDto>.Failure(
                Error.Validation("requests.invalid_assignee", "The assignee is invalid.", "assignedTo", "AssignedTo cannot exceed 256 characters."));
        }

        var request = await requests.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return Result<RequestDto>.Failure(NotFound(id));
        }

        var previousAssignee = request.AssignedTo;
        var now = clock.UtcNow.ToUniversalTime();
        try
        {
            request.Assign(command.AssignedTo, now);
        }
        catch (InvalidOperationException exception)
        {
            return Result<RequestDto>.Failure(Error.Conflict("requests.not_assignable", exception.Message));
        }

        await AddEventAsync(
            "RequestAssigned",
            request,
            "Responsable actualizado",
            request.AssignedTo is null
                ? $"La solicitud «{request.Title}» quedó sin responsable."
                : $"La solicitud «{request.Title}» fue asignada a {request.AssignedTo}.",
            new { previousAssignee, assignedTo = request.AssignedTo, assignedBy = currentUser.UserId },
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<RequestDto>.Success(Map(request));
    }

    public async Task<Result<RequestDto>> AddCommentAsync(
        long id,
        AddCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = await requests.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return Result<RequestDto>.Failure(NotFound(id));
        }

        var access = EnsureCanAccess(request);
        if (access is not null)
        {
            return Result<RequestDto>.Failure(access);
        }

        if (string.IsNullOrWhiteSpace(command.Content) || command.Content.Trim().Length > 2_000)
        {
            return Result<RequestDto>.Failure(
                Error.Validation("requests.invalid_comment", "The comment is invalid.", "content", "Content is required and cannot exceed 2000 characters."));
        }

        var now = clock.UtcNow.ToUniversalTime();
        request.AddComment(currentUser.UserId, command.Content, now);
        await AddEventAsync(
            "CommentAdded",
            request,
            "Nuevo comentario",
            $"Se agregó un comentario a «{request.Title}».",
            new { authorId = currentUser.UserId, content = command.Content.Trim() },
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<RequestDto>.Success(Map(request));
    }

    public async Task<Result<IReadOnlyList<RequestHistoryDto>>> GetHistoryAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var request = await requests.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return Result<IReadOnlyList<RequestHistoryDto>>.Failure(NotFound(id));
        }

        var access = EnsureCanAccess(request);
        if (access is not null)
        {
            return Result<IReadOnlyList<RequestHistoryDto>>.Failure(access);
        }

        IReadOnlyList<RequestHistoryDto> history = request.History
            .OrderByDescending(item => item.ChangedAt)
            .Select(item => new RequestHistoryDto(
                item.Id,
                item.RequestId,
                item.PreviousStatus,
                item.NewStatus,
                item.ChangedBy,
                item.ChangedAt))
            .ToArray();
        return Result<IReadOnlyList<RequestHistoryDto>>.Success(history);
    }

    private async Task AddEventAsync(
        string eventType,
        Request request,
        string title,
        string message,
        object data,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var eventId = Guid.NewGuid();
        var envelope = new RequestEventEnvelope(
            eventId,
            eventType,
            occurredAt,
            request.Id,
            request.CreatedBy,
            title,
            message,
            correlation.CorrelationId,
            data);
        var payload = JsonSerializer.Serialize(envelope, EventJsonOptions);
        await outbox.AddOutboxAsync(
            OutboxMessage.Create(eventId, eventType, payload, occurredAt, correlation.CorrelationId),
            cancellationToken);
    }

    private Error? EnsureCurrentUser() => currentUser.IsAuthenticated && !string.IsNullOrWhiteSpace(currentUser.UserId)
        ? null
        : Error.Forbidden("auth.user_required", "An authenticated user is required.");

    private Error? EnsureCanAccess(Request request)
    {
        var authentication = EnsureCurrentUser();
        if (authentication is not null)
        {
            return authentication;
        }

        if (IsGlobalReader() ||
            currentUser.IsInRole("Employee") &&
            string.Equals(request.CreatedBy, currentUser.UserId, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Error.Forbidden(
            "requests.forbidden",
            "You do not have permission to access this request.");
    }

    private bool IsGlobalReader() =>
        currentUser.IsInRole("Agent") || currentUser.IsInRole("Administrator");

    private static Error? ValidateDetails(
        string? title,
        string? description,
        Domain.Enums.RequestCategory category,
        Domain.Enums.RequestPriority priority)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length is < 5 or > 160)
        {
            return Error.Validation("requests.invalid_title", "The title is invalid.", "title", "Title must contain between 5 and 160 characters.");
        }

        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length is < 20 or > 4_000)
        {
            return Error.Validation("requests.invalid_description", "The description is invalid.", "description", "Description must contain between 20 and 4000 characters.");
        }

        if (!Enum.IsDefined(category))
        {
            return Error.Validation("requests.invalid_category", "The category is invalid.", "category", "Use a supported category.");
        }

        return !Enum.IsDefined(priority)
            ? Error.Validation("requests.invalid_priority", "The priority is invalid.", "priority", "Use a supported priority.")
            : null;
    }

    private static Error NotFound(long id) =>
        Error.NotFound("requests.not_found", $"Request {id} was not found.");

    private static RequestDto Map(Request request) => new(
        request.Id,
        request.Title,
        request.Description,
        request.Category,
        request.Priority,
        request.Status,
        request.CreatedBy,
        request.AssignedTo,
        request.DueAt,
        request.CreatedAt,
        request.UpdatedAt,
        request.Comments
            .OrderBy(item => item.CreatedAt)
            .Select(item => new RequestCommentDto(
                item.Id,
                item.RequestId,
                item.AuthorId,
                item.Content,
                item.CreatedAt))
            .ToArray());

    private sealed record RequestEventEnvelope(
        Guid EventId,
        string EventType,
        DateTimeOffset OccurredAt,
        long RequestId,
        string UserId,
        string Title,
        string Message,
        string? CorrelationId,
        object Data);
}
