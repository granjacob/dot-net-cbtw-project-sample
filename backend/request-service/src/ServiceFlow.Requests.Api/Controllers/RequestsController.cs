using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceFlow.Requests.Api.Authentication;
using ServiceFlow.Requests.Application.Models;
using ServiceFlow.Requests.Application.Services;
using ServiceFlow.Requests.Domain.Enums;

namespace ServiceFlow.Requests.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/requests")]
public sealed class RequestsController(IRequestService requestService) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CreateRequests)]
    [ProducesResponseType<RequestDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RequestDto>> Create(
        CreateRequestCommand command,
        CancellationToken cancellationToken)
    {
        var result = await requestService.CreateAsync(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : this.Failure(result);
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<RequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RequestDto>>> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] RequestStatus? status = null,
        [FromQuery] RequestPriority? priority = null,
        [FromQuery] RequestCategory? category = null,
        [FromQuery] string? assignedTo = null,
        [FromQuery] string? createdBy = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        var filter = new RequestFilter
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Status = status,
            Priority = priority,
            Category = category,
            AssignedTo = assignedTo,
            CreatedBy = createdBy,
            SortBy = sortBy,
            SortDirection = sortDirection
        };
        var result = await requestService.SearchAsync(filter, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await requestService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.MutateRequests)]
    public async Task<ActionResult<RequestDto>> Update(
        long id,
        UpdateRequestCommand command,
        CancellationToken cancellationToken)
    {
        var result = await requestService.UpdateAsync(id, command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }

    [HttpPatch("{id:long}/status")]
    [Authorize(Policy = AuthorizationPolicies.MutateRequests)]
    public async Task<ActionResult<RequestDto>> ChangeStatus(
        long id,
        ChangeRequestStatusCommand command,
        CancellationToken cancellationToken)
    {
        var result = await requestService.ChangeStatusAsync(id, command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }

    [HttpPatch("{id:long}/assignment")]
    [Authorize(Policy = AuthorizationPolicies.MutateRequests)]
    public async Task<ActionResult<RequestDto>> Assign(
        long id,
        AssignRequestCommand command,
        CancellationToken cancellationToken)
    {
        var result = await requestService.AssignAsync(id, command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }

    [HttpPost("{id:long}/comments")]
    public async Task<ActionResult<RequestDto>> AddComment(
        long id,
        AddCommentCommand command,
        CancellationToken cancellationToken)
    {
        var result = await requestService.AddCommentAsync(id, command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }

    [HttpGet("{id:long}/history")]
    [ProducesResponseType<IReadOnlyList<RequestHistoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RequestHistoryDto>>> GetHistory(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await requestService.GetHistoryAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }
}
