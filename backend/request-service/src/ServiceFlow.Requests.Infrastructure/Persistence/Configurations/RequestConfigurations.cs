using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Requests.Domain.Entities;
using ServiceFlow.Requests.Domain.Enums;

namespace ServiceFlow.Requests.Infrastructure.Persistence.Configurations;

public sealed class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.ToTable("Requests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedNever();
        builder.Property(request => request.Title).HasMaxLength(160).IsRequired();
        builder.Property(request => request.Description).HasMaxLength(4_000).IsRequired();
        builder.Property(request => request.Category).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(request => request.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(request => request.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(request => request.AssignedTo).HasMaxLength(256);
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex(request => request.Status);
        builder.HasIndex(request => request.Priority);
        builder.HasIndex(request => request.Category);
        builder.HasIndex(request => request.CreatedAt);
        builder.HasIndex(request => request.AssignedTo);

        builder.HasMany(request => request.Comments)
            .WithOne()
            .HasForeignKey(comment => comment.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(request => request.Comments)
            .HasField("_comments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(request => request.History)
            .WithOne()
            .HasForeignKey(history => history.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(request => request.History)
            .HasField("_history")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(
            new
            {
                Id = 1001L,
                Title = "Acceso bloqueado al portal de proveedores",
                Description = "Desde esta mañana el portal rechaza mis credenciales y necesito registrar una orden de compra urgente.",
                Category = RequestCategory.SystemAccess,
                Priority = RequestPriority.High,
                Status = RequestStatus.InProgress,
                CreatedBy = "employee@serviceflow.local",
                AssignedTo = "agent@serviceflow.local",
                CreatedAt = new DateTimeOffset(2026, 7, 20, 14, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2026, 7, 20, 15, 0, 0, TimeSpan.Zero),
                DueAt = new DateTimeOffset(2026, 7, 21, 14, 0, 0, TimeSpan.Zero)
            },
            new
            {
                Id = 1002L,
                Title = "Mantenimiento preventivo de portátil",
                Description = "El equipo presenta calentamiento y ruido constante del ventilador; solicito una revisión preventiva completa.",
                Category = RequestCategory.Maintenance,
                Priority = RequestPriority.Medium,
                Status = RequestStatus.Open,
                CreatedBy = "employee@serviceflow.local",
                AssignedTo = (string?)null,
                CreatedAt = new DateTimeOffset(2026, 7, 21, 13, 30, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2026, 7, 21, 13, 30, 0, TimeSpan.Zero),
                DueAt = new DateTimeOffset(2026, 7, 24, 13, 30, 0, TimeSpan.Zero)
            },
            new
            {
                Id = 1003L,
                Title = "Interrupción en línea de empaque principal",
                Description = "La línea de empaque se detuvo y no responde al reinicio estándar, afectando toda la operación del turno.",
                Category = RequestCategory.OperationalIncident,
                Priority = RequestPriority.Critical,
                Status = RequestStatus.Pending,
                CreatedBy = "employee@serviceflow.local",
                AssignedTo = "agent@serviceflow.local",
                CreatedAt = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2026, 7, 22, 12, 30, 0, TimeSpan.Zero),
                DueAt = new DateTimeOffset(2026, 7, 22, 16, 0, 0, TimeSpan.Zero)
            });
    }
}

public sealed class RequestCommentConfiguration : IEntityTypeConfiguration<RequestComment>
{
    public void Configure(EntityTypeBuilder<RequestComment> builder)
    {
        builder.ToTable("RequestComments");
        builder.HasKey(comment => comment.Id);
        builder.Property(comment => comment.Id).ValueGeneratedOnAdd();
        builder.Property(comment => comment.AuthorId).HasMaxLength(256).IsRequired();
        builder.Property(comment => comment.Content).HasMaxLength(2_000).IsRequired();
        builder.HasIndex(comment => new { comment.RequestId, comment.CreatedAt });

        builder.HasData(new
        {
            Id = 1L,
            RequestId = 1001L,
            AuthorId = "agent@serviceflow.local",
            Content = "Estamos revisando el bloqueo con el equipo de identidades.",
            CreatedAt = new DateTimeOffset(2026, 7, 20, 15, 0, 0, TimeSpan.Zero)
        });
    }
}

public sealed class RequestHistoryConfiguration : IEntityTypeConfiguration<RequestHistory>
{
    public void Configure(EntityTypeBuilder<RequestHistory> builder)
    {
        builder.ToTable("RequestHistory");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedOnAdd();
        builder.Property(history => history.PreviousStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(history => history.NewStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(history => history.ChangedBy).HasMaxLength(256).IsRequired();
        builder.HasIndex(history => new { history.RequestId, history.ChangedAt });

        builder.HasData(
            new
            {
                Id = 1L,
                RequestId = 1001L,
                PreviousStatus = RequestStatus.Open,
                NewStatus = RequestStatus.InProgress,
                ChangedBy = "agent@serviceflow.local",
                ChangedAt = new DateTimeOffset(2026, 7, 20, 15, 0, 0, TimeSpan.Zero)
            },
            new
            {
                Id = 2L,
                RequestId = 1002L,
                PreviousStatus = RequestStatus.Open,
                NewStatus = RequestStatus.Open,
                ChangedBy = "employee@serviceflow.local",
                ChangedAt = new DateTimeOffset(2026, 7, 21, 13, 30, 0, TimeSpan.Zero)
            },
            new
            {
                Id = 3L,
                RequestId = 1003L,
                PreviousStatus = RequestStatus.Open,
                NewStatus = RequestStatus.Pending,
                ChangedBy = "agent@serviceflow.local",
                ChangedAt = new DateTimeOffset(2026, 7, 22, 12, 30, 0, TimeSpan.Zero)
            });
    }
}

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.EventType).HasMaxLength(100).IsRequired();
        builder.Property(message => message.Payload).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(2_000);
        builder.Property(message => message.CorrelationId).HasMaxLength(128);
        builder.HasIndex(message => new { message.ProcessedAt, message.OccurredAt });
    }
}
