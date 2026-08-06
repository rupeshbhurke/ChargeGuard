namespace ChargeGuard.Analytics;

/// <summary>
/// Represents a single battery reading for analytics.
/// </summary>
public class BatteryReading
{
    public DateTime Timestamp { get; set; }
    public int BatteryPercentage { get; set; }
    public bool IsCharging { get; set; }
    public bool IsAcConnected { get; set; }
    public bool IsBatteryAvailable { get; set; }
}