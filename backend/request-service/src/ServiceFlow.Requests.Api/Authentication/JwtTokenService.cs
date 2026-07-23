using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ServiceFlow.Requests.Api.Authentication;

public sealed record LoginToken(string Token, DateTime ExpiresAt, LoginUser User);

public sealed record LoginUser(string Email, string Name, string Role);

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JwtOptions _options = options.Value;

    public LoginToken Create(DemoUser user)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.ExpirationMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new System.Security.Claims.Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new System.Security.Claims.Claim(JwtRegisteredClaimNames.Email, user.Email),
            new System.Security.Claims.Claim("name", user.Name),
            new System.Security.Claims.Claim("role", user.Role),
            new System.Security.Claims.Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            now,
            expiresAt,
            credentials);

        return new LoginToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            new LoginUser(user.Email, user.Name, user.Role));
    }
}
