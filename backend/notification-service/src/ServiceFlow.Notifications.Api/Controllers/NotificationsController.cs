using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceFlow.Notifications.Api.Realtime;
using ServiceFlow.Notifications.Application.Contracts;
using ServiceFlow.Notifications.Application.Services;

namespace ServiceFlow.Notifications.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(NotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<NotificationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> Get(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        var result = await notificationService.SearchAsync(
            CurrentUserId,
            isRead,
            page,
            pageSize,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread")]
    [ProducesResponseType<PagedResult<NotificationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetUnread(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await notificationService.GetUnreadAsync(
            CurrentUserId,
            page,
            pageSize,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        var count = await notificationService.CountUnreadAsync(CurrentUserId, cancellationToken);
        return Ok(new { count });
    }

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType<NotificationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationDto>> MarkAsRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        var notification = await notificationService.MarkAsReadAsync(
            id,
            CurrentUserId,
            cancellationToken);

        return notification is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Notification not found",
                Detail = "The notification does not exist or does not belong to the current user."
            })
            : Ok(notification);
    }

    [HttpPatch("read-all")]
    public async Task<ActionResult<object>> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var updated = await notificationService.MarkAllAsReadAsync(
            CurrentUserId,
            cancellationToken);
        return Ok(new { updated });
    }

    private string CurrentUserId => UserIdentity.GetUserId(User);
}
