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

        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.BootstrapServers), "Kafka:BootstrapServers is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Topic), "Kafka:Topic is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DeadLetterTopic), "Kafka:DeadLetterTopic is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.GroupId), "Kafka:GroupId is required.")
            .Validate(options => options.MaxProcessingAttempts is > 0 and <= 10, "Kafka:MaxProcessingAttempts must be between 1 and 10.")
            .ValidateOnStart();
        services.Configure<DatabaseInitializationOptions>(
            configuration.GetSection(DatabaseInitializationOptions.SectionName));

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<INotificationRepository, EfNotificationRepository>();
        services.AddHostedService<KafkaNotificationConsumer>();

        return services;
    }
}
