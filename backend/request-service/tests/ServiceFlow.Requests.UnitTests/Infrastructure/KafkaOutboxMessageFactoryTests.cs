using System.Text;
using ServiceFlow.Requests.Domain.Entities;
using ServiceFlow.Requests.Infrastructure.Messaging;

namespace ServiceFlow.Requests.UnitTests.Infrastructure;

public sealed class KafkaOutboxMessageFactoryTests
{
    [Fact]
    public void Create_UsesRequestIdAsPartitionKeyAndPreservesMetadata()
    {
        var eventId = Guid.NewGuid();
        const string payload = """{"eventId":"00000000-0000-0000-0000-000000000001","eventType":"RequestCreated","requestId":42}""";
        var outbox = OutboxMessage.Create(eventId, "RequestCreated", payload, DateTimeOffset.UtcNow, "correlation-123");

        var message = KafkaOutboxMessageFactory.Create(outbox);

        Assert.Equal("42", message.Key);
        Assert.Equal(payload, message.Value);
        Assert.Equal(eventId.ToString(), ReadHeader(message.Headers, "event-id"));
        Assert.Equal("RequestCreated", ReadHeader(message.Headers, "event-type"));
        Assert.Equal("correlation-123", ReadHeader(message.Headers, "correlation-id"));
        Assert.Equal("application/json", ReadHeader(message.Headers, "content-type"));
    }

    [Fact]
    public void Create_InvalidPayload_FallsBackToEventIdPartitionKey()
    {
        var eventId = Guid.NewGuid();
        var outbox = OutboxMessage.Create(eventId, "RequestCreated", "not-json", DateTimeOffset.UtcNow, null);

        var message = KafkaOutboxMessageFactory.Create(outbox);

        Assert.Equal(eventId.ToString(), message.Key);
    }

    private static string ReadHeader(Confluent.Kafka.Headers headers, string name) =>
        Encoding.UTF8.GetString(headers.GetLastBytes(name));
}
