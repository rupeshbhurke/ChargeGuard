namespace ChargeGuard.Core.Battery;

/// <summary>
/// Represents a snapshot of the current battery and power status.
/// </summary>
public class BatterySnapshot
{
    /// <summary>
    /// Gets whether AC power is currently connected.
    /// </summary>
    public bool IsAcPowerConnected { get; init; }

    /// <summary>
    /// Gets the current battery percentage (1-100), or null if unknown.
    /// </summary>
    public int? BatteryPercentage { get; init; }

    /// <summary>
    /// Gets whether the battery is currently charging.
    /// </summary>
    public bool IsCharging { get; init; }

    /// <summary>
    /// Gets whether a battery is present in the system.
    /// </summary>
    public bool IsBatteryAvailable { get; init; }

    /// <summary>
    /// Gets the timestamp when this snapshot was taken.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a battery snapshot for when no battery is available.
    /// </summary>
    public static BatterySnapshot CreateUnavailable()
    {
        return new BatterySnapshot
        {
            IsAcPowerConnected = true, // Assume AC if no battery
            BatteryPercentage = null,
            IsCharging = false,
            IsBatteryAvailable = false
        };
    }

    /// <summary>
    /// Returns a string representation of the battery status.
    /// </summary>
    public override string ToString()
    {
        if (!IsBatteryAvailable)
        {
            return "Battery unavailable";
        }

        var percentage = BatteryPercentage?.ToString() ?? "Unknown";
        var powerSource = IsAcPowerConnected ? "AC power" : "Battery";
        var charging = IsCharging ? " charging" : "";

        return $"{percentage}% on {powerSource}{charging}";
    }

    /// <summary>
    /// Creates a battery snapshot from a platform-specific status object.
    /// This is a placeholder for platform-specific implementations.
    /// </summary>
    public static BatterySnapshot FromNativeStatus(object nativeStatus)
    {
        // This will be implemented in platform-specific code
        // For now, return unavailable as fallback
        return CreateUnavailable();
    }
}