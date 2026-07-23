using Microsoft.EntityFrameworkCore;
using ServiceFlow.Requests.Application.Abstractions;
using ServiceFlow.Requests.Application.Models;
using ServiceFlow.Requests.Domain.Entities;
using ServiceFlow.Requests.Domain.Enums;

namespace ServiceFlow.Requests.Infrastructure.Persistence;

public sealed class RequestRepository(RequestDbContext dbContext) : IRequestRepository
{
    public Task<Request?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        dbContext.Requests
            .AsSplitQuery()
            .Include(request => request.Comments)
            .Include(request => request.History)
            .SingleOrDefaultAsync(request => request.Id == id, cancellationToken);

    public async Task<PagedResult<Request>> SearchAsync(
        RequestFilter filter,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Request> query = dbContext.Requests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            var code = search.StartsWith("SF-", StringComparison.OrdinalIgnoreCase)
                ? search[3..]
                : search;
            var hasRequestId = long.TryParse(code, out var requestId);
            query = query.Where(request =>
                request.Title.Contains(search) ||
                request.Description.Contains(search) ||
                hasRequestId && request.Id == requestId);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(request => request.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(request => request.Priority == filter.Priority.Value);
        }

        if (filter.Category.HasValue)
        {
            query = query.Where(request => request.Category == filter.Category.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
        {
            query = query.Where(request => request.AssignedTo == filter.AssignedTo.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.CreatedBy))
        {
            query = query.Where(request => request.CreatedBy == filter.CreatedBy.Trim());
        }

        var total = await query.LongCountAsync(cancellationToken);
        var descending = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = ApplyOrdering(query, filter.SortBy, descending);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<Request>(items, total, filter.Page, filter.PageSize);
    }

    internal static IQueryable<Request> ApplyOrdering(
        IQueryable<Request> query,
        string sortBy,
        bool descending) => sortBy.ToLowerInvariant() switch
        {
            "title" => descending ? query.OrderByDescending(request => request.Title) : query.OrderBy(request => request.Title),
            "priority" => descending
                ? query.OrderByDescending(request =>
                    request.Priority == RequestPriority.Critical ? 4 :
                    request.Priority == RequestPriority.High ? 3 :
                    request.Priority == RequestPriority.Medium ? 2 : 1)
                : query.OrderBy(request =>
                    request.Priority == RequestPriority.Critical ? 4 :
                    request.Priority == RequestPriority.High ? 3 :
                    request.Priority == RequestPriority.Medium ? 2 : 1),
            "status" => descending ? query.OrderByDescending(request => request.Status) : query.OrderBy(request => request.Status),
            "updatedat" => descending ? query.OrderByDescending(request => request.UpdatedAt) : query.OrderBy(request => request.UpdatedAt),
            "dueat" => descending ? query.OrderByDescending(request => request.DueAt) : query.OrderBy(request => request.DueAt),
            _ => descending ? query.OrderByDescending(request => request.CreatedAt) : query.OrderBy(request => request.CreatedAt)
        };

    public async Task AddAsync(Request request, CancellationToken cancellationToken = default) =>
        await dbContext.Requests.AddAsync(request, cancellationToken);
}

public sealed class OutboxRepository(RequestDbContext dbContext) : IOutboxRepository
{
    public async Task AddOutboxAsync(OutboxMessage message, CancellationToken cancellationToken = default) =>
        await dbContext.OutboxMessages.AddAsync(message, cancellationToken);

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxAsync(
        int batchSize,
        CancellationToken cancellationToken = default) =>
        await dbContext.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.OccurredAt)
            .Take(Math.Clamp(batchSize, 1, 500))
            .ToListAsync(cancellationToken);
}
