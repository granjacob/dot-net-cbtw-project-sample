using System.Text.Json;
using ServiceFlow.Requests.Application.Abstractions;
using ServiceFlow.Requests.Application.Common;
using ServiceFlow.Requests.Application.Models;
using ServiceFlow.Requests.Application.Services;
using ServiceFlow.Requests.Application.Sla;
using ServiceFlow.Requests.Domain.Entities;
using ServiceFlow.Requests.Domain.Enums;

namespace ServiceFlow.Requests.UnitTests.Application;

public sealed class RequestServiceTests
{
    [Fact]
    public async Task Create_PersistsRequestAndMatchingOutboxEventInOneUnitOfWork()
    {
        var requestRepository = new FakeRequestRepository();
        var outboxRepository = new FakeOutboxRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(requestRepository, outboxRepository, unitOfWork);
        var command = new CreateRequestCommand(
            "Unable to use payroll portal",
            "The payroll portal displays an authorization error after signing in.",
            RequestCategory.SystemAccess,
            RequestPriority.Critical);

        var result = await service.CreateAsync(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(requestRepository.Added);
        Assert.Equal(9001, result.Value!.Id);
        Assert.Equal(1, unitOfWork.SaveCalls);
        var message = Assert.Single(outboxRepository.Messages);
        using var payload = JsonDocument.Parse(message.Payload);
        Assert.Equal(message.Id, payload.RootElement.GetProperty("eventId").GetGuid());
        Assert.Equal("RequestCreated", payload.RootElement.GetProperty("eventType").GetString());
        Assert.Equal(9001, payload.RootElement.GetProperty("requestId").GetInt64());
        Assert.Equal("employee@serviceflow.local", payload.RootElement.GetProperty("userId").GetString());
        Assert.Equal("test-correlation", payload.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task Create_InvalidDetails_ReturnsValidationWithoutWriting()
    {
        var requestRepository = new FakeRequestRepository();
        var outboxRepository = new FakeOutboxRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(requestRepository, outboxRepository, unitOfWork);

        var result = await service.CreateAsync(new CreateRequestCommand(
            "bad",
            "too short",
            RequestCategory.TechnicalSupport,
            RequestPriority.Low), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("requests.invalid_title", result.Error!.Code);
        Assert.Null(requestRepository.Added);
        Assert.Empty(outboxRepository.Messages);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task EveryMutation_AddsAnOutboxEventBeforeSaving()
    {
        var now = new DateTimeOffset(2026, 7, 22, 17, 0, 0, TimeSpan.Zero);
        var existing = Request.Create(
            77,
            "Printer is unavailable on third floor",
            "The shared printer is offline for every employee on the third floor.",
            RequestCategory.TechnicalSupport,
            RequestPriority.Medium,
            "employee@serviceflow.local",
            now.AddHours(-1),
            now.AddDays(3));
        var requestRepository = new FakeRequestRepository(existing);
        var outboxRepository = new FakeOutboxRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(requestRepository, outboxRepository, unitOfWork);
        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.True((await service.UpdateAsync(77, new UpdateRequestCommand(
            "Printer remains unavailable on third floor",
            "The shared printer remains offline after checking its power and network cable.",
            RequestCategory.Maintenance,
            RequestPriority.High), cancellationToken)).IsSuccess);
        Assert.True((await service.AssignAsync(
            77,
            new AssignRequestCommand("agent@serviceflow.local"),
            cancellationToken)).IsSuccess);
        Assert.True((await service.ChangeStatusAsync(
            77,
            new ChangeRequestStatusCommand(RequestStatus.InProgress),
            cancellationToken)).IsSuccess);
        Assert.True((await service.AddCommentAsync(
            77,
            new AddCommentCommand("A technician will inspect the device shortly."),
            cancellationToken)).IsSuccess);

        Assert.Equal(4, unitOfWork.SaveCalls);
        Assert.Equal(
            ["RequestUpdated", "RequestAssigned", "RequestStatusChanged", "CommentAdded"],
            outboxRepository.Messages.Select(message => message.EventType));
    }

    [Fact]
    public async Task Employee_CannotReadHistoryOrCommentOnAnotherEmployeesRequest()
    {
        var foreignRequest = ExistingRequest(createdBy: "owner@serviceflow.local");
        var repository = new FakeRequestRepository(foreignRequest);
        var outbox = new FakeOutboxRepository();
        var unitOfWork = new FakeUnitOfWork();
        var employee = new FakeCurrentUser("employee@serviceflow.local", "Employee");
        var service = CreateService(repository, outbox, unitOfWork, employee);
        var cancellationToken = TestContext.Current.CancellationToken;

        var detail = await service.GetByIdAsync(foreignRequest.Id, cancellationToken);
        var history = await service.GetHistoryAsync(foreignRequest.Id, cancellationToken);
        var comment = await service.AddCommentAsync(
            foreignRequest.Id,
            new AddCommentCommand("I should not be allowed to add this comment."),
            cancellationToken);

        Assert.Equal(ErrorType.Forbidden, detail.Error?.Type);
        Assert.Equal(ErrorType.Forbidden, history.Error?.Type);
        Assert.Equal(ErrorType.Forbidden, comment.Error?.Type);
        Assert.Empty(outbox.Messages);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Employee_SearchForcesCreatedByAndIgnoresRequestedOwner()
    {
        var repository = new FakeRequestRepository();
        var service = CreateService(
            repository,
            new FakeOutboxRepository(),
            new FakeUnitOfWork(),
            new FakeCurrentUser("employee@serviceflow.local", "Employee"));

        var result = await service.SearchAsync(new RequestFilter
        {
            CreatedBy = "another-user@serviceflow.local",
            Page = 2,
            PageSize = 10,
            SortBy = "priority",
            SortDirection = "asc"
        }, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastFilter);
        Assert.Equal("employee@serviceflow.local", repository.LastFilter!.CreatedBy);
        Assert.Equal(2, repository.LastFilter.Page);
        Assert.Equal("priority", repository.LastFilter.SortBy);
    }

    [Fact]
    public async Task Agent_HasGlobalReadAndCommentAccess()
    {
        var foreignRequest = ExistingRequest(createdBy: "owner@serviceflow.local");
        var repository = new FakeRequestRepository(foreignRequest);
        var outbox = new FakeOutboxRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            repository,
            outbox,
            unitOfWork,
            new FakeCurrentUser("agent@serviceflow.local", "Agent"));
        var cancellationToken = TestContext.Current.CancellationToken;

        var detail = await service.GetByIdAsync(foreignRequest.Id, cancellationToken);
        var comment = await service.AddCommentAsync(
            foreignRequest.Id,
            new AddCommentCommand("The assigned agent can add operational context."),
            cancellationToken);

        Assert.True(detail.IsSuccess);
        Assert.True(comment.IsSuccess);
        Assert.Equal("CommentAdded", Assert.Single(outbox.Messages).EventType);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    private static RequestService CreateService(
        FakeRequestRepository requests,
        FakeOutboxRepository outbox,
        FakeUnitOfWork unitOfWork,
        ICurrentUser? currentUser = null) => new(
        requests,
        outbox,
        unitOfWork,
        new SlaStrategyFactory(),
        new FakeClock(),
        currentUser ?? new FakeCurrentUser("employee@serviceflow.local", "Employee"),
        new FakeCorrelation(),
        new FakeIdGenerator());

    private static Request ExistingRequest(string createdBy) => Request.Create(
        77,
        "Printer is unavailable on third floor",
        "The shared printer is offline for every employee on the third floor.",
        RequestCategory.TechnicalSupport,
        RequestPriority.Medium,
        createdBy,
        new DateTimeOffset(2026, 7, 22, 16, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 25, 16, 0, 0, TimeSpan.Zero));

    private sealed class FakeRequestRepository : IRequestRepository
    {
        private readonly Request? _existing;

        public FakeRequestRepository(Request? existing = null)
        {
            _existing = existing;
        }

        public Request? Added { get; private set; }
        public RequestFilter? LastFilter { get; private set; }
        public Task<Request?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_existing?.Id == id ? _existing : null);
        public Task<PagedResult<Request>> SearchAsync(RequestFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(CaptureFilter(filter));
        public Task AddAsync(Request request, CancellationToken cancellationToken = default)
        {
            Added = request;
            return Task.CompletedTask;
        }

        private PagedResult<Request> CaptureFilter(RequestFilter filter)
        {
            LastFilter = filter;
            return new PagedResult<Request>([], 0, filter.Page, filter.PageSize);
        }
    }

    private sealed class FakeOutboxRepository : IOutboxRepository
    {
        public List<OutboxMessage> Messages { get; } = [];
        public Task AddOutboxAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxAsync(int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboxMessage>>(Messages);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 7, 22, 17, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeCurrentUser(string userId, params string[] roles) : ICurrentUser
    {
        public string UserId => userId;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => roles.Contains(role, StringComparer.Ordinal);
    }

    private sealed class FakeCorrelation : ICorrelationIdAccessor
    {
        public string CorrelationId => "test-correlation";
    }

    private sealed class FakeIdGenerator : IRequestIdGenerator
    {
        public long NewId() => 9001;
    }
}
