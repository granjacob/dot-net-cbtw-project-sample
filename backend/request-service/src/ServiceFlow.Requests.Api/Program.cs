using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ServiceFlow.Requests.Api.Authentication;
using ServiceFlow.Requests.Api.Middleware;
using ServiceFlow.Requests.Application.Abstractions;
using ServiceFlow.Requests.Infrastructure;
using ServiceFlow.Requests.Infrastructure.Health;
using ServiceFlow.Requests.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions.TryAdd("correlationId", context.HttpContext.TraceIdentifier);
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddOpenApi();

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options => Encoding.UTF8.GetByteCount(options.Key) >= 32, "Jwt:Key must contain at least 32 bytes.")
    .Validate(options => options.ExpirationMinutes is > 0 and <= 1_440, "Jwt:ExpirationMinutes must be between 1 and 1440.")
    .ValidateOnStart();
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is required.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Authentication required",
                    Detail = "Provide a valid bearer token.",
                    Type = "https://httpstatuses.com/401",
                    Extensions = { ["correlationId"] = context.HttpContext.TraceIdentifier }
                }, context.HttpContext.RequestAborted);
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "Your role does not allow this operation.",
                    Type = "https://httpstatuses.com/403",
                    Extensions = { ["correlationId"] = context.HttpContext.TraceIdentifier }
                }, context.HttpContext.RequestAborted);
            }
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.CreateRequests, policy => policy.RequireRole(
        DemoUsers.EmployeeRole,
        DemoUsers.AgentRole,
        DemoUsers.AdministratorRole))
    .AddPolicy(AuthorizationPolicies.MutateRequests, policy => policy.RequireRole(
        DemoUsers.AgentRole,
        DemoUsers.AdministratorRole));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<ICurrentUser>(provider => provider.GetRequiredService<CurrentUserAccessor>());
builder.Services.AddScoped<CorrelationIdAccessor>();
builder.Services.AddScoped<ICorrelationIdAccessor>(provider => provider.GetRequiredService<CorrelationIdAccessor>());
builder.Services.AddSingleton<JwtTokenService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000", "http://localhost:5173"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddRequestInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("requests-database", tags: ["ready"]);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi().AllowAnonymous();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
}).AllowAnonymous();

await app.Services.EnsureDatabaseCreatedAsync();
await app.RunAsync();

public partial class Program;
