using ServiceFlow.Requests.Domain.Entities;
using ServiceFlow.Requests.Domain.Enums;

namespace ServiceFlow.Requests.UnitTests.Domain;

public sealed class RequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_NormalizesValuesAndCreatesInitialHistory()
    {
        var request = CreateRequest();

        Assert.Equal(42, request.Id);
        Assert.Equal("VPN access is unavailable", request.Title);
        Assert.Equal(RequestStatus.Open, request.Status);
        Assert.Equal(Now, request.CreatedAt);
        var history = Assert.Single(request.History);
        Assert.Equal(RequestStatus.Open, history.PreviousStatus);
        Assert.Equal(RequestStatus.Open, history.NewStatus);
    }

    [Fact]
    public void ChangeStatus_ValidTransition_AppendsAuditEntry()
    {
        var request = CreateRequest();

        request.ChangeStatus(RequestStatus.InProgress, "agent@serviceflow.local", Now.AddMinutes(5));

        Assert.Equal(RequestStatus.InProgress, request.Status);
        Assert.Equal(2, request.History.Count);
        var audit = request.History.Last();
        Assert.Equal(RequestStatus.Open, audit.PreviousStatus);
        Assert.Equal(RequestStatus.InProgress, audit.NewStatus);
        Assert.Equal("agent@serviceflow.local", audit.ChangedBy);
    }

    [Fact]
    public void ChangeStatus_FromClosed_IsRejected()
    {
        var request = CreateRequest();
        request.ChangeStatus(RequestStatus.Closed, "agent@serviceflow.local", Now.AddMinutes(1));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            request.ChangeStatus(RequestStatus.Open, "agent@serviceflow.local", Now.AddMinutes(2)));

        Assert.Contains("Cannot transition", exception.Message);
    }

    [Fact]
    public void AddComment_TrimsContentAndUpdatesTimestamp()
    {
        var request = CreateRequest();

        var comment = request.AddComment(
            "agent@serviceflow.local",
            "  We are reviewing the incident.  ",
            Now.AddMinutes(10));

        Assert.Equal("We are reviewing the incident.", comment.Content);
        Assert.Equal(request.Id, comment.RequestId);
        Assert.Equal(Now.AddMinutes(10), request.UpdatedAt);
    }

    private static Request CreateRequest() => Request.Create(
        42,
        "  VPN access is unavailable  ",
        "The corporate VPN rejects the connection from my managed laptop.",
        RequestCategory.TechnicalSupport,
        RequestPriority.High,
        "employee@serviceflow.local",
        Now,
        Now.AddDays(1));
}
