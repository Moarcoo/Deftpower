using Deftpower.Onb.Assessment.Models;

namespace Deftpower.Onb.Assessment.Tests;

// Only missing or invalid values are validated here, time rules are covered by ChargingSessionTests.
public class CreateChargingSessionRequestTests
{
    [Fact]
    public void IsValid_AllValuesSet_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        var request = CreateRequest(startTime: now, endTime: now.AddHours(2));

        Assert.True(request.IsValid());
    }

    [Fact]
    public void IsValid_NoEndTime_ReturnsTrue()
    {
        var request = CreateRequest(endTime: null);

        Assert.True(request.IsValid());
    }

    [Fact]
    public void IsValid_StartTimeInThePast_ReturnsTrue()
    {
        // Time rules are not the request's responsibility.
        var request = CreateRequest(startTime: DateTime.UtcNow.AddDays(-1));

        Assert.True(request.IsValid());
    }

    [Fact]
    public void IsValid_DefaultStartTime_ReturnsFalse()
    {
        var request = CreateRequest(startTime: default(DateTime));

        Assert.False(request.IsValid());
    }

    [Fact]
    public void IsValid_DefaultEndTime_ReturnsFalse()
    {
        var request = CreateRequest(endTime: default(DateTime));

        Assert.False(request.IsValid());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_MissingUserId_ReturnsFalse(string? userId)
    {
        var request = CreateRequest(userId: userId!);

        Assert.False(request.IsValid());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_MissingChargePointId_ReturnsFalse(string? chargePointId)
    {
        var request = CreateRequest(chargePointId: chargePointId!);

        Assert.False(request.IsValid());
    }

    private static CreateChargingSessionRequest CreateRequest(
        string userId = "user-1",
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
}
