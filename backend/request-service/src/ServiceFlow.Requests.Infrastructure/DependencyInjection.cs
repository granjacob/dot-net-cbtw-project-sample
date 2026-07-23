using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceFlow.Requests.Application.Abstractions;
using ServiceFlow.Requests.Application.Services;
using ServiceFlow.Requests.Application.Sla;
using ServiceFlow.Requests.Infrastructure.Messaging;
using ServiceFlow.Requests.Infrastructure.Persistence;

namespace ServiceFlow.Requests.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRequestInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RequestsDatabase")
            ?? throw new InvalidOperationException("Connection string 'RequestsDatabase' is required.");

        services.AddDbContext<RequestDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.EnableRetryOnFailure(6, TimeSpan.FromSeconds(10), null)));
        services.AddScoped<IRequestRepository, RequestRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<RequestDbContext>());

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISlaStrategyFactory, SlaStrategyFactory>();
        var requestIdNode = configuration.GetValue<int?>("RequestId:NodeId") ?? 0;
        if (requestIdNode is < 0 or > RequestIdGenerator.MaxNodeId)
        {
            throw new InvalidOperationException(
                $"RequestId:NodeId must be between 0 and {RequestIdGenerator.MaxNodeId}.");
        }

        services.AddSingleton<IRequestIdGenerator>(provider => new RequestIdGenerator(
            provider.GetRequiredService<TimeProvider>(),
            requestIdNode));
        services.AddScoped<IRequestService, RequestService>();

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "RabbitMq:Host is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Queue), "RabbitMq:Queue is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DeadLetterExchange), "RabbitMq:DeadLetterExchange is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DeadLetterQueue), "RabbitMq:DeadLetterQueue is required.")
            .Validate(options => options.Port is > 0 and <= 65535, "RabbitMq:Port is invalid.")
            .Validate(options => options.BatchSize is > 0 and <= 500, "RabbitMq:BatchSize must be between 1 and 500.")
            .ValidateOnStart();
        services.AddHostedService<OutboxPublisher>();

        return services;
    }
}
