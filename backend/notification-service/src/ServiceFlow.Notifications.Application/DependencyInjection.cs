using Microsoft.Extensions.DependencyInjection;
using ServiceFlow.Notifications.Application.Services;

namespace ServiceFlow.Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<NotificationService>();
        return services;
    }
}
