using System.Net;
using System.Net.Http.Json;
using Deftpower.Onb.Assessment.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Deftpower.Onb.Assessment.Tests;

public class ChargingSessionEndpointsSmokeTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private readonly string _userId = $"user-{Guid.NewGuid()}";

    [Fact]
    public async Task CreateUpdateAndGetSessions_HappyPath()
    {
        var startTime = DateTime.UtcNow;

        var olderSessionId = await CreateSession("cp-1", startTime);
        var newerSessionId = await CreateSession("cp-2", startTime.AddMinutes(1));

        var endTime = startTime.AddHours(1);
        var updateResponse = await _client.PutAsJsonAsync("/api/sessions", new UpdateChargingSessionRequest
        {
            SessionId = olderSessionId,
            EndTime = endTime,
            EnergyKwh = 12.5,
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/users/{_userId}/sessions");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var sessions = await getResponse.Content.ReadFromJsonAsync<List<ChargingSessionResponse>>();
        Assert.NotNull(sessions);
        Assert.Collection(sessions,
            newer =>
            {
                Assert.Equal(newerSessionId, newer.SessionId);
                Assert.Equal(_userId, newer.UserId);
                Assert.Equal("cp-2", newer.ChargePointId);
                Assert.Equal(startTime.AddMinutes(1), newer.StartTime);
                Assert.Null(newer.EndTime);
                Assert.Equal(0, newer.EnergyKwh);
            },
            older =>
            {
                Assert.Equal(olderSessionId, older.SessionId);
                Assert.Equal(_userId, older.UserId);
                Assert.Equal("cp-1", older.ChargePointId);
                Assert.Equal(startTime, older.StartTime);
                Assert.Equal(endTime, older.EndTime);
                Assert.Equal(12.5, older.EnergyKwh);
            });
    }

    [Fact]
    public async Task CreateSession_BlacklistedUser_ReturnsUnauthorized()
    {
        const string blacklistedUserId = "user-1";

        var response = await _client.PostAsJsonAsync($"/api/users/{blacklistedUserId}/sessions", new CreateChargingSessionRequest
        {
            UserId = blacklistedUserId,
            ChargePointId = "cp-1",
            StartTime = DateTime.UtcNow,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> CreateSession(string chargePointId, DateTime startTime)
    {
        var response = await _client.PostAsJsonAsync($"/api/users/{_userId}/sessions", new CreateChargingSessionRequest
        {
            UserId = _userId,
            ChargePointId = chargePointId,
            StartTime = startTime,
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateChargingSessionResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.SessionId));
        return body.SessionId;
    }
}
