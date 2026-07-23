using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Notifications.Domain.Entities;

namespace ServiceFlow.Notifications.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.UserId).HasMaxLength(256).IsRequired();
        builder.Property(notification => notification.Type).HasMaxLength(100).IsRequired();
        builder.Property(notification => notification.Title).HasMaxLength(200).IsRequired();
        builder.Property(notification => notification.Message).HasMaxLength(2000).IsRequired();
        builder.Property(notification => notification.CreatedAt).HasPrecision(7).IsRequired();
        builder.Property(notification => notification.EventId).IsRequired();
        builder.Property(notification => notification.RequestId);

        builder.HasIndex(notification => notification.EventId).IsUnique();
        builder.HasIndex(notification => notification.RequestId);
        builder.HasIndex(notification => new
        {
            notification.UserId,
            notification.IsRead,
            notification.CreatedAt
        });
    }
}
