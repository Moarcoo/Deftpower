namespace Deftpower.Onb.Assessment.Models;

public class UpdateChargingSessionRequest
{
    public required string SessionId { get; set; }

    public required DateTime? EndTime { get; set; }

    public required double EnergyKwh { get; set; } // in kWh

    // Only checks for missing or invalid values, business rules are validated by ChargingSession
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(SessionId)) return false;
        if (EndTime.HasValue && EndTime.Value == default) return false;
        if (!double.IsFinite(EnergyKwh) || EnergyKwh < 0) return false;
        return true;
    }
}
