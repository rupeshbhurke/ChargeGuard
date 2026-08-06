using ChargeGuard.Charging;

namespace ChargeGuard.Notifications;

/// <summary>
/// Interface for alert notification implementations.
/// </summary>
public interface IAlertNotifier
{
    /// <summary>
    /// Shows an alert to the user.
    /// </summary>
    void ShowAlert(ChargingAlertDecision decision);

    /// <summary>
    /// Updates the tray icon tooltip with current status.
    /// </summary>
    void UpdateTooltip(string status);
}
