using ServiceFlow.Requests.Domain.Entities;

namespace ServiceFlow.Requests.Application.Abstractions;

public interface IOutboxRepository
{
    Task AddOutboxAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxAsync(int batchSize, CancellationToken cancellationToken = default);
}
