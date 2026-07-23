using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceFlow.Requests.Api.Authentication;

namespace ServiceFlow.Requests.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(JwtTokenService tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginToken>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginToken> Login(LoginRequest request)
    {
        var user = DemoUsers.Validate(request.Email, request.Password);
        if (user is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The email or password is incorrect.",
                type: "https://httpstatuses.com/401");
        }

        return Ok(tokens.Create(user));
    }
}

public sealed record LoginRequest(
    [param: Required, EmailAddress, MaxLength(256)] string Email,
    [param: Required, MaxLength(128)] string Password);
