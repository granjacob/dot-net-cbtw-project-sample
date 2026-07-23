using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ServiceFlow.Requests.Domain.Entities;
using ServiceFlow.Requests.Infrastructure.Persistence;

namespace ServiceFlow.Requests.UnitTests.Infrastructure;

public sealed class RequestDbContextModelTests
{
    [Fact]
    public void Model_ContainsOutboxRelationshipsAndDemoSeed()
    {
        var options = new DbContextOptionsBuilder<RequestDbContext>()
            .UseSqlServer("Server=(local);Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var dbContext = new RequestDbContext(options);

        var model = dbContext.GetService<IDesignTimeModel>().Model;
        var requestEntity = model.FindEntityType(typeof(Request));
        var outboxEntity = model.FindEntityType(typeof(OutboxMessage));
        Assert.NotNull(requestEntity);
        Assert.NotNull(outboxEntity);

        Assert.Equal("Requests", requestEntity!.GetTableName());
        Assert.Equal("OutboxMessages", outboxEntity!.GetTableName());
        Assert.Equal(3, requestEntity.GetSeedData().Count());
        Assert.Equal(2, requestEntity.GetNavigations().Count());
    }

    [Fact]
    public void PriorityOrdering_TranslatesToNumericCaseInsteadOfAlphabeticalEnumText()
    {
        var options = new DbContextOptionsBuilder<RequestDbContext>()
            .UseSqlServer("Server=(local);Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var dbContext = new RequestDbContext(options);

        var sql = RequestRepository.ApplyOrdering(
                dbContext.Requests.AsNoTracking(),
                "priority",
                descending: true)
            .ToQueryString();

        Assert.Contains("CASE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Priority", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DESC", sql, StringComparison.OrdinalIgnoreCase);
    }
}
