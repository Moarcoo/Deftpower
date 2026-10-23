using Deftpower.Onb.Assessment.Models;

namespace Deftpower.Onb.Assessment.Tests;

// Only missing or invalid values are validated here, time rules are covered by ChargingSessionTests.
public class UpdateChargingSessionRequestTests
{
    [Fact]
    public void IsValid_EndTimeSet_ReturnsTrue()
    {
        var request = CreateRequest(endTime: DateTime.UtcNow.AddHours(1));

        Assert.True(request.IsValid());
    }

    [Fact]
    public void IsValid_NoEndTime_ReturnsTrue()
    {
        var request = CreateRequest(endTime: null);

        Assert.True(request.IsValid());
    }

    [Fact]
    public void IsValid_EndTimeInThePast_ReturnsTrue()
    {
        // Time rules are not the request's responsibility.
        var request = CreateRequest(endTime: DateTime.UtcNow.AddMinutes(-30));

        Assert.True(request.IsValid());
    }

    [Fact]
    public void IsValid_DefaultEndTime_ReturnsFalse()
    {
        var request = CreateRequest(endTime: default(DateTime));

        Assert.False(request.IsValid());
    }

    [Fact]
    public void IsValid_ZeroEnergy_ReturnsTrue()
    {
        // Comparing against the stored energy is ChargingSession's responsibility.
        var request = CreateRequest(energyKwh: 0);

        Assert.True(request.IsValid());
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void IsValid_InvalidEnergy_ReturnsFalse(double energyKwh)
    {
        var request = CreateRequest(energyKwh: energyKwh);

        Assert.False(request.IsValid());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_MissingSessionId_ReturnsFalse(string? sessionId)
    {
        var request = CreateRequest(sessionId: sessionId!);

        Assert.False(request.IsValid());
    }

    private static UpdateChargingSessionRequest CreateRequest(
        string sessionId = "session-1",
        DateTime? endTime = null,
        double energyKwh = 1) =>
        new()
        {
            SessionId = sessionId,
            EndTime = endTime,
            EnergyKwh = energyKwh
        };
}
