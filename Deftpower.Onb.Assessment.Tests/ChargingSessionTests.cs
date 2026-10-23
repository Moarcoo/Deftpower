using Deftpower.Onb.Assessment.Exceptions;
using Deftpower.Onb.Assessment.Models;
using Deftpower.Onb.Assessment.Services;

namespace Deftpower.Onb.Assessment.Tests;

public class ChargingSessionTests
{
    [Fact]
    public void Create_StartTimeNow_ReturnsSessionWithValues()
    {
        var now = DateTime.UtcNow;

        var session = ChargingSession.Create("user-1", "cp-1", now, now.AddHours(2));

        Assert.False(string.IsNullOrWhiteSpace(session.SessionId));
        Assert.Equal("user-1", session.UserId);
        Assert.Equal("cp-1", session.ChargePointId);
        Assert.Equal(now, session.StartTime);
        Assert.Equal(now.AddHours(2), session.EndTime);
        Assert.Equal(0, session.EnergyKwh);
    }

    [Fact]
    public void Create_StartTimeWithinGracePeriod_ReturnsSession()
    {
        var session = Create(startTime: DateTime.UtcNow.AddMinutes(-4));

        Assert.NotNull(session);
    }

    [Fact]
    public void Create_StartTimeInTheFuture_ReturnsSession()
    {
        var session = Create(startTime: DateTime.UtcNow.AddDays(1));

        Assert.NotNull(session);
    }

    [Fact]
    public void Create_StartTimeBeforeGracePeriod_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => Create(startTime: DateTime.UtcNow.AddMinutes(-6)));
    }

    [Fact]
    public void Create_StartTimeDaysInThePast_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => Create(startTime: DateTime.UtcNow.AddDays(-1)));
    }

    [Fact]
    public void Create_EndTimeBeforeStartTime_ThrowsDomainValidationException()
    {
        var now = DateTime.UtcNow;

        Assert.Throws<DomainValidationException>(() => Create(startTime: now, endTime: now.AddMinutes(-1)));
    }

    [Fact]
    public void Create_EndTimeInThePast_ThrowsDomainValidationException()
    {
        var now = DateTime.UtcNow;

        Assert.Throws<DomainValidationException>(() => Create(startTime: now.AddMinutes(-4), endTime: now.AddMinutes(-1)));
    }

    [Fact]
    public void Update_EndTimeInTheFuture_UpdatesEndTime()
    {
        var session = Create(startTime: DateTime.UtcNow.AddMinutes(-4));
        var endTime = DateTime.UtcNow.AddHours(1);

        session.Update(endTime, 1);

        Assert.Equal(endTime, session.EndTime);
    }

    [Fact]
    public void Update_NoEndTime_ClearsEndTime()
    {
        var now = DateTime.UtcNow;
        var session = Create(startTime: now.AddMinutes(-4), endTime: now.AddHours(1));

        session.Update(null, 1);

        Assert.Null(session.EndTime);
    }

    [Fact]
    public void Update_SessionNotStarted_ThrowsDomainValidationException()
    {
        var session = Create(startTime: DateTime.UtcNow.AddDays(1));

        Assert.Throws<DomainValidationException>(() => session.Update(null, 1));
    }

    [Fact]
    public void Update_EndTimeBeforeStartTime_ThrowsDomainValidationException()
    {
        var session = Create(startTime: DateTime.UtcNow.AddMinutes(-4));

        Assert.Throws<DomainValidationException>(() => session.Update(session.StartTime.AddMinutes(-1), 1));
    }

    [Fact]
    public void Update_EndTimeInThePast_ThrowsDomainValidationException()
    {
        var session = Create(startTime: DateTime.UtcNow.AddMinutes(-4));

        Assert.Throws<DomainValidationException>(() => session.Update(DateTime.UtcNow.AddMinutes(-1), 1));
    }

    [Fact]
    public void Update_HigherEnergy_UpdatesEnergy()
    {
        var session = Create(startTime: DateTime.UtcNow.AddMinutes(-4));

        session.Update(null, 2.5);

        Assert.Equal(2.5, session.EnergyKwh);
    }

    [Fact]
    public void Update_SequentialHigherEnergy_UpdatesEnergyEachTime()
    {
        var session = Create(startTime: DateTime.UtcNow.AddMinutes(-4));

        session.Update(null, 1);
        session.Update(null, 3);

        Assert.Equal(3, session.EnergyKwh);
    }

    [Fact]
    public void Update_ZeroEnergyOnNewSession_ThrowsDomainValidationException()
    {
        // A new session starts at 0 kWh, so even the first update must report more energy.
        var session = Create(startTime: DateTime.UtcNow.AddMinutes(-4));

        Assert.Throws<DomainValidationException>(() => session.Update(null, 0));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(1.9)]
    [InlineData(0)]
    public void Update_EnergyNotHigherThanPreviousUpdate_ThrowsDomainValidationException(double energyKwh)
    {
        var session = Create(startTime: DateTime.UtcNow.AddMinutes(-4));
        session.Update(null, 2);

        Assert.Throws<DomainValidationException>(() => session.Update(null, energyKwh));
    }

    [Fact]
    public void Update_InvalidEndTime_LeavesSessionUnchanged()
    {
        var now = DateTime.UtcNow;
        var session = Create(startTime: now.AddMinutes(-4));
        session.Update(now.AddHours(1), 2);

        Assert.Throws<DomainValidationException>(() => session.Update(now.AddMinutes(-1), 3));

        Assert.Equal(now.AddHours(1), session.EndTime);
        Assert.Equal(2, session.EnergyKwh);
    }

    [Fact]
    public void Update_InvalidEnergy_LeavesSessionUnchanged()
    {
        var now = DateTime.UtcNow;
        var session = Create(startTime: now.AddMinutes(-4));
        session.Update(now.AddHours(1), 2);

        Assert.Throws<DomainValidationException>(() => session.Update(now.AddHours(2), 1));

        Assert.Equal(now.AddHours(1), session.EndTime);
        Assert.Equal(2, session.EnergyKwh);
    }

    private static ChargingSession Create(DateTime? startTime = null, DateTime? endTime = null) =>
        ChargingSession.Create("user-1", "cp-1", startTime ?? DateTime.UtcNow, endTime);
}
