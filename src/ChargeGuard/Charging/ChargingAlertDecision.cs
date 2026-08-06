namespace ChargeGuard.Charging;

/// <summary>
/// Represents a decision to show an alert to the user.
/// </summary>
public class ChargingAlertDecision
{
    /// <summary>
    /// Gets the type of alert.
    /// </summary>
    public ChargingAlertType AlertType { get; init; }

    /// <summary>
    /// Gets the message to display to the user.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current battery percentage.
    /// </summary>
    public int? BatteryPercentage { get; init; }

    /// <summary>
    /// Gets the active target percentage.
    /// </summary>
    public int TargetPercentage { get; init; }

    /// <summary>
    /// Gets whether sound should be played.
    /// </summary>
    public bool PlaySound { get; init; }

    public ChargingAlertDecision(
        ChargingAlertType alertType,
        string message,
        int? batteryPercentage,
        int targetPercentage,
        bool playSound = true)
    {
        AlertType = alertType;
        Message = message;
        BatteryPercentage = batteryPercentage;
        TargetPercentage = targetPercentage;
        PlaySound = playSound;
    }
}
