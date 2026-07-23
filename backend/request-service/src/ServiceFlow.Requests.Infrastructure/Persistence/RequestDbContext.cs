using Microsoft.EntityFrameworkCore;
using ServiceFlow.Requests.Application.Abstractions;
using ServiceFlow.Requests.Domain.Entities;

namespace ServiceFlow.Requests.Infrastructure.Persistence;

public sealed class RequestDbContext(DbContextOptions<RequestDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<RequestComment> RequestComments => Set<RequestComment>();
    public DbSet<RequestHistory> RequestHistory => Set<RequestHistory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("requests");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RequestDbContext).Assembly);
    }
}
