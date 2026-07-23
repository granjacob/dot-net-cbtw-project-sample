using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using ServiceFlow.Notifications.Api.Middleware;
using ServiceFlow.Notifications.Api.Realtime;
using ServiceFlow.Notifications.Application;
using ServiceFlow.Notifications.Application.Abstractions;
using ServiceFlow.Notifications.Infrastructure;
using ServiceFlow.Notifications.Infrastructure.Health;
using ServiceFlow.Notifications.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ServiceFlow",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ServiceFlow.Client",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token)
                    && context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders(CorrelationIdMiddleware.HeaderName);
    });
});

builder.Services.Configure<RealtimeOptions>(
    builder.Configuration.GetSection(RealtimeOptions.SectionName));
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PayloadSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddSingleton<INotificationRealtimePublisher, SignalRNotificationPublisher>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("notifications-database", tags: ["ready"]);

var app = builder.Build();

await DatabaseInitializer.EnsureCreatedWithRetryAsync(
    app.Services,
    app.Lifetime.ApplicationStopping);

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi().AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
}).AllowAnonymous();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

public partial class Program;
