namespace Deftpower.Onb.Assessment.Models;

public class CreateChargingSessionRequest
{
    public required string UserId { get; init; }

    public required string ChargePointId { get; init; }

    public required DateTime StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    // Only checks for missing or invalid values, business rules are validated by ChargingSession
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(UserId)) return false;
        if (string.IsNullOrWhiteSpace(ChargePointId)) return false;
        if (StartTime == default) return false;
        if (EndTime.HasValue && EndTime.Value == default) return false;
        return true;
    }
}
