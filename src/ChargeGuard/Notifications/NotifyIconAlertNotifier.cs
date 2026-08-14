using System.Windows.Forms;
using ChargeGuard.Charging;
using ChargeGuard.Logging;

namespace ChargeGuard.Notifications;

/// <summary>
/// Alert notifier using WinForms NotifyIcon for balloon notifications.
/// </summary>
public class NotifyIconAlertNotifier : IAlertNotifier
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ISoundPlayer _soundPlayer;
    private readonly IAppLogger _logger;
    private readonly Settings.ChargeGuardSettings _settings;

    public NotifyIconAlertNotifier(NotifyIcon notifyIcon, ISoundPlayer soundPlayer, IAppLogger logger, Settings.ChargeGuardSettings settings)
    {
        _notifyIcon = notifyIcon ?? throw new ArgumentNullException(nameof(notifyIcon));
        _soundPlayer = soundPlayer ?? throw new ArgumentNullException(nameof(soundPlayer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
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

            // Show balloon notification
            _notifyIcon.BalloonTipTitle = GetAlertTitle(decision.AlertType);
            _notifyIcon.BalloonTipText = decision.Message;
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip((int)_settings.NotificationTimeout.TotalMilliseconds);

            _logger.LogInfo($"Alert shown: {decision.AlertType} - {decision.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to show alert notification", ex);
        }
    }

    public void UpdateTooltip(string status)
    {
        try
        {
            _notifyIcon.Text = $"ChargeGuard — {status}";
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update tooltip", ex);
        }
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
}
