using ServiceFlow.Requests.Application.Common;
using ServiceFlow.Requests.Application.Models;

namespace ServiceFlow.Requests.Application.Services;

public interface IRequestService
{
    Task<Result<RequestDto>> CreateAsync(CreateRequestCommand command, CancellationToken cancellationToken = default);
    Task<Result<RequestDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<RequestDto>>> SearchAsync(RequestFilter filter, CancellationToken cancellationToken = default);
    Task<Result<RequestDto>> UpdateAsync(long id, UpdateRequestCommand command, CancellationToken cancellationToken = default);
    Task<Result<RequestDto>> ChangeStatusAsync(long id, ChangeRequestStatusCommand command, CancellationToken cancellationToken = default);
    Task<Result<RequestDto>> AssignAsync(long id, AssignRequestCommand command, CancellationToken cancellationToken = default);
    Task<Result<RequestDto>> AddCommentAsync(long id, AddCommentCommand command, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RequestHistoryDto>>> GetHistoryAsync(long id, CancellationToken cancellationToken = default);
}
