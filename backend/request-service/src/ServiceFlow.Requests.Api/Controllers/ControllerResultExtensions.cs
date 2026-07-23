using Microsoft.AspNetCore.Mvc;
using ServiceFlow.Requests.Application.Common;

namespace ServiceFlow.Requests.Api.Controllers;

internal static class ControllerResultExtensions
{
    public static ActionResult Failure<T>(this ControllerBase controller, Result<T> result)
    {
        var error = result.Error ?? new Error(
            "unexpected_error",
            "The operation failed.",
            ErrorType.Conflict);
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        ProblemDetails problem = error.Details is null
            ? new ProblemDetails()
            : new ValidationProblemDetails(error.Details.ToDictionary(pair => pair.Key, pair => pair.Value));
        problem.Status = status;
        problem.Title = error.Type switch
        {
            ErrorType.Validation => "Validation failed",
            ErrorType.NotFound => "Resource not found",
            ErrorType.Forbidden => "Forbidden",
            ErrorType.Conflict => "Request conflict",
            _ => "Request failed"
        };
        problem.Detail = error.Message;
        problem.Type = $"https://httpstatuses.com/{status}";
        problem.Extensions["code"] = error.Code;
        problem.Extensions["correlationId"] = controller.HttpContext.TraceIdentifier;
        return new ObjectResult(problem) { StatusCode = status };
    }
}
