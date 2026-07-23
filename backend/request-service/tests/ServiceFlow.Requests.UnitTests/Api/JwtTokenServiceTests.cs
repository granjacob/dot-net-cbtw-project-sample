using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceFlow.Requests.Api.Authentication;

namespace ServiceFlow.Requests.UnitTests.Api;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void Create_ProducesAValidatedTokenWithRoleClaims()
    {
        var jwtOptions = new JwtOptions
        {
            Issuer = "ServiceFlow",
            Audience = "ServiceFlow.Client",
            Key = "unit-test-signing-key-that-is-long-enough-123456",
            ExpirationMinutes = 60
        };
        var service = new JwtTokenService(Options.Create(jwtOptions), new FixedTimeProvider());
        var agent = DemoUsers.All.Single(user => user.Role == DemoUsers.AgentRole);

        var response = service.Create(agent);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(response.Token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = false,
            NameClaimType = "name",
            RoleClaimType = "role"
        }, out _);

        Assert.Equal(agent.Email, principal.FindFirst("sub")?.Value);
        Assert.Equal(agent.Name, principal.Identity?.Name);
        Assert.True(principal.IsInRole(DemoUsers.AgentRole));
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 7, 22, 18, 0, 0, TimeSpan.Zero);
    }
}
