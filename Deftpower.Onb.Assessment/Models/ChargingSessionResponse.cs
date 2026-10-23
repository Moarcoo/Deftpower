namespace Deftpower.Onb.Assessment.Models;

public class ChargingSessionResponse
{
    public required string SessionId { get; init; }

    public required string UserId { get; init; }

    public required string ChargePointId { get; init; }

    public required DateTime StartTime { get; init; }

    public DateTime? EndTime { get; init; } 

    public double EnergyKwh { get; init; }
}