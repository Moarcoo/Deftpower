using Deftpower.Onb.Assessment.Data;
using Deftpower.Onb.Assessment.Exceptions;
using Deftpower.Onb.Assessment.Models;
using Deftpower.Onb.Assessment.Pagination;
using Deftpower.Onb.Assessment.Repositories;
using Deftpower.Onb.Assessment.Services;
using Microsoft.EntityFrameworkCore;

namespace Deftpower.Onb.Assessment.Tests;

public class ChargingSessionServiceTests
{
    private readonly DateTime _startTime = DateTime.UtcNow;

    private readonly ChargingSessionService _sut;
    private readonly string _userId = $"user-{Guid.NewGuid()}";

    public ChargingSessionServiceTests()
    {
        // A fresh in-memory database per test keeps tests isolated.
        var options = new DbContextOptionsBuilder<ChargingSessionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _sut = new ChargingSessionService(new ChargingSessionRepository(new ChargingSessionDbContext(options)), new UserRepository());
    }

    [Fact]
    public async Task CreateChargingSession_ValidRequest_ReturnsSessionId()
    {
        var response = await _sut.CreateChargingSession(CreateRequest(), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(response.SessionId));
    }

    [Fact]
    public async Task CreateChargingSession_CalledTwice_ReturnsDistinctSessionIds()
    {
        var first = await _sut.CreateChargingSession(CreateRequest(), CancellationToken.None);
        var second = await _sut.CreateChargingSession(CreateRequest(), CancellationToken.None);

        Assert.NotEqual(first.SessionId, second.SessionId);
    }

    [Fact]
    public async Task CreateChargingSession_ValidRequest_StoresSessionWithRequestData()
    {
        var request = CreateRequest(chargePointId: "cp-42", endTime: _startTime.AddHours(2));

        var response = await _sut.CreateChargingSession(request, CancellationToken.None);

        var session = Assert.Single(await GetSessionsForCurrentUser());
        Assert.Equal(response.SessionId, session.SessionId);
        Assert.Equal(_userId, session.UserId);
        Assert.Equal("cp-42", session.ChargePointId);
        Assert.Equal(_startTime, session.StartTime);
        Assert.Equal(_startTime.AddHours(2), session.EndTime);
    }

    public static TheoryData<string, CreateChargingSessionRequest> InvalidCreateRequests => new()
    {
        { "empty user id", InvalidCreateRequest(userId: "") },
        { "whitespace user id", InvalidCreateRequest(userId: "   ") },
        { "empty charge point id", InvalidCreateRequest(chargePointId: "") },
        { "default start time", InvalidCreateRequest(startTime: default(DateTime)) },
        { "default end time", InvalidCreateRequest(endTime: default(DateTime)) }
    };

    [Theory]
    [MemberData(nameof(InvalidCreateRequests))]
    public async Task CreateChargingSession_InvalidRequest_ThrowsArgumentException(
        string scenario, CreateChargingSessionRequest request)
    {
        _ = scenario;

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateChargingSession(request, CancellationToken.None));
    }

    public static TheoryData<string, CreateChargingSessionRequest> CreateRequestsBreakingDomainRules => new()
    {
        { "start time before grace period", InvalidCreateRequest(startTime: DateTime.UtcNow.AddMinutes(-10)) },
        { "end time before start time", InvalidCreateRequest(endTime: DateTime.UtcNow.AddMinutes(-1)) },
        {
            "end time in the past",
            InvalidCreateRequest(startTime: DateTime.UtcNow.AddMinutes(-4), endTime: DateTime.UtcNow.AddMinutes(-1))
        }
    };

    [Theory]
    [MemberData(nameof(CreateRequestsBreakingDomainRules))]
    public async Task CreateChargingSession_RequestBreakingDomainRule_ThrowsDomainValidationException(
        string scenario, CreateChargingSessionRequest request)
    {
        _ = scenario;

        await Assert.ThrowsAsync<DomainValidationException>(() => _sut.CreateChargingSession(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateChargingSession_InvalidRequest_DoesNotStoreSession()
    {
        var request = CreateRequest(endTime: _startTime.AddMinutes(-1));

        await Assert.ThrowsAsync<DomainValidationException>(() => _sut.CreateChargingSession(request, CancellationToken.None));

        Assert.Empty(await GetSessionsForCurrentUser());
    }

    [Theory]
    [InlineData("user-1")]
    [InlineData("Q")]
    public async Task CreateChargingSession_BlacklistedUser_ThrowsUnauthorizedAccessExceptionAndDoesNotStoreSession(string userId)
    {
        var request = InvalidCreateRequest(userId: userId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.CreateChargingSession(request, CancellationToken.None));

        Assert.Empty(await _sut.GetAllChargingSessions(userId, new PaginationParams(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateChargingSession_ExistingSession_UpdatesEndTimeAndEnergyWithoutAddingSession()
    {
        var created = await _sut.CreateChargingSession(CreateRequest(), CancellationToken.None);
        var endTime = _startTime.AddHours(3);

        await _sut.UpdateChargingSession(UpdateRequest(created.SessionId, endTime, energyKwh: 7.5), CancellationToken.None);

        var session = Assert.Single(await GetSessionsForCurrentUser());
        Assert.Equal(created.SessionId, session.SessionId);
        Assert.Equal(endTime, session.EndTime);
        Assert.Equal(7.5, session.EnergyKwh);
    }

    [Fact]
    public async Task UpdateChargingSession_SequentialUpdates_StoresLatestEnergy()
    {
        var created = await _sut.CreateChargingSession(CreateRequest(), CancellationToken.None);

        await _sut.UpdateChargingSession(UpdateRequest(created.SessionId, endTime: null, energyKwh: 1), CancellationToken.None);
        await _sut.UpdateChargingSession(UpdateRequest(created.SessionId, endTime: null, energyKwh: 4), CancellationToken.None);

        var session = Assert.Single(await GetSessionsForCurrentUser());
        Assert.Equal(4, session.EnergyKwh);
    }

    [Fact]
    public async Task UpdateChargingSession_EnergyNotHigherThanPreviousUpdate_LeavesSessionUnchanged()
    {
        var created = await _sut.CreateChargingSession(CreateRequest(), CancellationToken.None);
        await _sut.UpdateChargingSession(UpdateRequest(created.SessionId, endTime: null, energyKwh: 4), CancellationToken.None);
        var request = UpdateRequest(created.SessionId, endTime: null, energyKwh: 4);

        await Assert.ThrowsAsync<DomainValidationException>(() => _sut.UpdateChargingSession(request, CancellationToken.None));

        var session = Assert.Single(await GetSessionsForCurrentUser());
        Assert.Equal(4, session.EnergyKwh);
    }

    [Fact]
    public async Task UpdateChargingSession_NegativeEnergy_ThrowsArgumentException()
    {
        var created = await _sut.CreateChargingSession(CreateRequest(), CancellationToken.None);
        var request = UpdateRequest(created.SessionId, endTime: null, energyKwh: -1);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpdateChargingSession(request, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateChargingSession_EndTimeBeforeStoredStartTime_ThrowsDomainValidationException()
    {
        var created = await _sut.CreateChargingSession(CreateRequest(), CancellationToken.None);
        var request = UpdateRequest(created.SessionId, endTime: _startTime.AddMinutes(-1));

        await Assert.ThrowsAsync<DomainValidationException>(() => _sut.UpdateChargingSession(request, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateChargingSession_EndTimeInThePast_ThrowsDomainValidationException()
    {
        var created = await _sut.CreateChargingSession(CreateRequest(startTime: _startTime.AddMinutes(-4)), CancellationToken.None);
        var request = UpdateRequest(created.SessionId, endTime: _startTime.AddMinutes(-1));

        await Assert.ThrowsAsync<DomainValidationException>(() => _sut.UpdateChargingSession(request, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateChargingSession_InvalidRequest_LeavesSessionUnchanged()
    {
        var created = await _sut.CreateChargingSession(CreateRequest(endTime: _startTime.AddHours(2)), CancellationToken.None);
        var request = UpdateRequest(created.SessionId, endTime: _startTime.AddMinutes(-1));

        await Assert.ThrowsAsync<DomainValidationException>(() => _sut.UpdateChargingSession(request, CancellationToken.None));

        var session = Assert.Single(await GetSessionsForCurrentUser());
        Assert.Equal(_startTime.AddHours(2), session.EndTime);
    }

    [Fact]
    public async Task UpdateChargingSession_MissingSessionId_ThrowsArgumentException()
    {
        var request = UpdateRequest("", endTime: _startTime.AddHours(1));

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpdateChargingSession(request, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateChargingSession_UnknownSessionId_ThrowsArgumentNullException()
    {
        var request = UpdateRequest(Guid.NewGuid().ToString(), endTime: _startTime.AddHours(1));

        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.UpdateChargingSession(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllChargingSessions_MultipleSessions_ReturnsNewestFirst()
    {
        // Start times stay within the grace period so every request is valid.
        var oldest = await _sut.CreateChargingSession(CreateRequest(startTime: _startTime.AddMinutes(-3)), CancellationToken.None);
        var newest = await _sut.CreateChargingSession(CreateRequest(startTime: _startTime.AddMinutes(-1)), CancellationToken.None);
        var middle = await _sut.CreateChargingSession(CreateRequest(startTime: _startTime.AddMinutes(-2)), CancellationToken.None);

        var sessions = await GetSessionsForCurrentUser();

        Assert.Equal(
            [newest.SessionId, middle.SessionId, oldest.SessionId],
            sessions.Select(s => s.SessionId));
    }

    [Fact]
    public async Task GetAllChargingSessions_SecondPage_ReturnsRemainingSessions()
    {
        var oldest = await _sut.CreateChargingSession(CreateRequest(startTime: _startTime.AddMinutes(-3)), CancellationToken.None);
        await _sut.CreateChargingSession(CreateRequest(startTime: _startTime.AddMinutes(-1)), CancellationToken.None);
        await _sut.CreateChargingSession(CreateRequest(startTime: _startTime.AddMinutes(-2)), CancellationToken.None);

        var sessions = await GetSessionsForCurrentUser(new PaginationParams { Page = 2, PageSize = 2 });

        var session = Assert.Single(sessions);
        Assert.Equal(oldest.SessionId, session.SessionId);
    }

    [Fact]
    public async Task GetAllChargingSessions_PageBeyondLastSession_ReturnsEmpty()
    {
        await _sut.CreateChargingSession(CreateRequest(), CancellationToken.None);

        var sessions = await GetSessionsForCurrentUser(new PaginationParams { Page = 2, PageSize = 1 });

        Assert.Empty(sessions);
    }

    public static TheoryData<string, PaginationParams> InvalidPaginationParams => new()
    {
        { "zero page", new PaginationParams { Page = 0 } },
        { "negative page", new PaginationParams { Page = -1 } },
        { "zero page size", new PaginationParams { PageSize = 0 } },
        { "page size above max", new PaginationParams { PageSize = 101 } }
    };

    [Theory]
    [MemberData(nameof(InvalidPaginationParams))]
    public async Task GetAllChargingSessions_InvalidPaginationParams_ThrowsArgumentException(
        string scenario, PaginationParams paginationParams)
    {
        _ = scenario;

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetAllChargingSessions(_userId, paginationParams, CancellationToken.None));
    }

    private CreateChargingSessionRequest CreateRequest(
        string chargePointId = "cp-1",
        DateTime? startTime = null,
        DateTime? endTime = null) =>
        new()
        {
            UserId = _userId,
            ChargePointId = chargePointId,
            StartTime = startTime ?? _startTime,
            EndTime = endTime
        };

    private static CreateChargingSessionRequest InvalidCreateRequest(
        string userId = "user-invalid",
        string chargePointId = "cp-1",
        DateTime? startTime = null,
        DateTime? endTime = null) =>
        new()
        {
            UserId = userId,
            ChargePointId = chargePointId,
            StartTime = startTime ?? DateTime.UtcNow,
            EndTime = endTime
        };

    private static UpdateChargingSessionRequest UpdateRequest(string sessionId, DateTime? endTime, double energyKwh = 1) =>
        new()
        {
            SessionId = sessionId,
            EndTime = endTime,
            EnergyKwh = energyKwh
        };

    private Task<IReadOnlyList<ChargingSessionResponse>> GetSessionsForCurrentUser(PaginationParams? paginationParams = null) =>
        _sut.GetAllChargingSessions(_userId, paginationParams ?? new PaginationParams(), CancellationToken.None);
}
