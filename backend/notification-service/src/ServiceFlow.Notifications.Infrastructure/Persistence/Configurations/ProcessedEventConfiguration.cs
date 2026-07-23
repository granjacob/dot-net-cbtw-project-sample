using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Notifications.Domain.Entities;

namespace ServiceFlow.Notifications.Infrastructure.Persistence.Configurations;

internal sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("ProcessedEvents");
        builder.HasKey(processedEvent => processedEvent.EventId);
        builder.Property(processedEvent => processedEvent.EventType).HasMaxLength(100).IsRequired();
        builder.Property(processedEvent => processedEvent.ProcessedAt).HasPrecision(7).IsRequired();
    }
}
