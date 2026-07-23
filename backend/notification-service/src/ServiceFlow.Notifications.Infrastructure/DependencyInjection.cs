using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceFlow.Notifications.Application.Abstractions;
using ServiceFlow.Notifications.Infrastructure.Configuration;
using ServiceFlow.Notifications.Infrastructure.Messaging;
using ServiceFlow.Notifications.Infrastructure.Persistence;

namespace ServiceFlow.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NotificationsDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'NotificationsDatabase' is not configured.");

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.HostName), "RabbitMq:HostName is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Queue), "RabbitMq:Queue is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DeadLetterExchange), "RabbitMq:DeadLetterExchange is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DeadLetterQueue), "RabbitMq:DeadLetterQueue is required.")
            .Validate(options => options.Port is > 0 and <= 65535, "RabbitMq:Port is invalid.")
            .ValidateOnStart();
        services.Configure<DatabaseInitializationOptions>(
            configuration.GetSection(DatabaseInitializationOptions.SectionName));

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<INotificationRepository, EfNotificationRepository>();
        services.AddHostedService<RabbitMqNotificationConsumer>();

        return services;
    }
}
