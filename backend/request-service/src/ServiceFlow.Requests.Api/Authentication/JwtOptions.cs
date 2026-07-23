namespace ServiceFlow.Requests.Api.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "ServiceFlow";
    public string Audience { get; init; } = "ServiceFlow.Client";
    public string Key { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 480;
}
