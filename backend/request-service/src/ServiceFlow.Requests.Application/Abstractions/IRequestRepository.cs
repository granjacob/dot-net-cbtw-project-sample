using ServiceFlow.Requests.Application.Models;
using ServiceFlow.Requests.Domain.Entities;

namespace ServiceFlow.Requests.Application.Abstractions;

public interface IRequestRepository
{
    Task<Request?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<PagedResult<Request>> SearchAsync(RequestFilter filter, CancellationToken cancellationToken = default);
    Task AddAsync(Request request, CancellationToken cancellationToken = default);
}
