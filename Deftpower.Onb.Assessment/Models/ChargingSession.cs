using Deftpower.Onb.Assessment.Exceptions;
using Deftpower.Onb.Assessment.Services;

using Deftpower.Onb.Assessment.Services;

namespace Deftpower.Onb.Assessment.Models;

public class ChargingSession
{
    // Don't allow sessions to start in the past, but for server and queue issues we allow 5 minutes in the past
    private static readonly TimeSpan StartTimeGracePeriod = TimeSpan.FromMinutes(5);

    public string SessionId { get; private set; } = Guid.NewGuid().ToString();

    public string UserId { get; private set; }

    public string ChargePointId { get; private set; }

    public DateTime StartTime { get; private set; }

    public DateTime? EndTime { get; private set; } // Nullable to allow ongoing sessions

    public double EnergyKwh { get; private set; } // in kWh

    public static ChargingSession Create(string userId, string chargePointId, DateTime startTime, DateTime? endTime)
    {
        var now = DateTime.UtcNow;

        if (startTime < now - StartTimeGracePeriod)
        {
            throw new DomainValidationException("StartTime must not be more than 5 minutes in the past", nameof(startTime));
        }

        ValidateEndTime(endTime, startTime, now);

        return new ChargingSession
        {
            UserId = userId,
            ChargePointId = chargePointId,
            StartTime = startTime,
            EndTime = endTime,
        };
    }

    public void Update(DateTime? endTime, double energyKwh)
    {
        var now = DateTime.UtcNow;

        if (StartTime > now)
        {
            throw new DomainValidationException("Session cannot be updated before it has started",  nameof(StartTime));
        }

        ValidateEndTime(endTime, StartTime, now);

        // The charge point meter only counts up, so every update must report more energy than the previous one
        if (energyKwh <= EnergyKwh)
        {
            throw new DomainValidationException("EnergyKwh must be greater than the current EnergyKwh",  nameof(EnergyKwh));
        }

        EndTime = endTime;
        EnergyKwh = energyKwh;
    }

    private static void ValidateEndTime(DateTime? endTime, DateTime startTime, DateTime now)
    {
        if (!endTime.HasValue) return;

        if (endTime.Value < startTime)
        {
            throw new DomainValidationException("EndTime must be greater than or equal to StartTime", nameof(endTime));
        }

        if (endTime.Value < now)
        {
            throw new DomainValidationException("EndTime must not be in the past", nameof(endTime));
        }
    }
}
