namespace ChargeGuard.Core.Charging;

/// <summary>
/// Represents a decision to show a charging alert.
/// </summary>
public class ChargingAlertDecision
{
    public ChargingAlertType AlertType { get; }
    public string Message { get; }
    public int CurrentPercentage { get; }
    public int TargetPercentage { get; }
    public bool PlaySound { get; }

    public ChargingAlertDecision(
        ChargingAlertType alertType,
        string message,
        int currentPercentage,
        int targetPercentage,
        bool playSound)
    {
        AlertType = alertType;
        Message = message;
        CurrentPercentage = currentPercentage;
        TargetPercentage = targetPercentage;
        PlaySound = playSound;
    }
}