using System.Windows.Forms;
using ChargeGuard.Charging;
using ChargeGuard.Logging;

namespace ChargeGuard.Notifications;

/// <summary>
/// Alert notifier using WinForms MessageBox for reliable dialog notifications.
/// </summary>
public class DialogAlertNotifier : IAlertNotifier
{
    private readonly ISoundPlayer _soundPlayer;
    private readonly IAppLogger _logger;

    public DialogAlertNotifier(ISoundPlayer soundPlayer, IAppLogger logger)
    {
        _soundPlayer = soundPlayer ?? throw new ArgumentNullException(nameof(soundPlayer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void ShowAlert(ChargingAlertDecision decision)
    {
        if (decision == null)
            return;

        try
        {
            // Play sound if enabled
            if (decision.PlaySound)
            {
                _soundPlayer.PlayNotificationSound();
            }

            // Show dialog notification (this works reliably in Windows 11)
            var title = GetAlertTitle(decision.AlertType);
            var message = decision.Message;
            var icon = GetAlertIcon(decision.AlertType);

            // Show dialog - this will appear on top and work reliably in Windows 11
            MessageBox.Show(message, title, MessageBoxButtons.OK, icon);

            _logger.LogInfo($"Alert shown: {decision.AlertType} - {decision.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to show alert notification", ex);
        }
    }

    public void UpdateTooltip(string status)
    {
        // Dialog notifier doesn't manage tray icon tooltip
        // This is handled by the NotifyIcon separately
    }

    private static string GetAlertTitle(ChargingAlertType alertType)
    {
        return alertType switch
        {
            ChargingAlertType.AdvanceWarning => "ChargeGuard — Advance Warning",
            ChargingAlertType.Target => "ChargeGuard — Charging Target Reached",
            ChargingAlertType.Reminder => "ChargeGuard — Reminder",
            ChargingAlertType.Escalation => "ChargeGuard — Escalation Warning",
            _ => "ChargeGuard"
        };
    }

    private static MessageBoxIcon GetAlertIcon(ChargingAlertType alertType)
    {
        return alertType switch
        {
            ChargingAlertType.AdvanceWarning => MessageBoxIcon.Information,
            ChargingAlertType.Target => MessageBoxIcon.Information,
            ChargingAlertType.Reminder => MessageBoxIcon.Warning,
            ChargingAlertType.Escalation => MessageBoxIcon.Warning,
            _ => MessageBoxIcon.Information
        };
    }
}