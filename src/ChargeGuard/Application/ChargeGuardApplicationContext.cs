using System.Windows.Forms;
using ChargeGuard.Battery;
using ChargeGuard.Charging;
using ChargeGuard.Logging;
using ChargeGuard.Notifications;
using ChargeGuard.Settings;
using Timer = System.Windows.Forms.Timer;
using WinFormsApplication = System.Windows.Forms.Application;
using MessageBox = System.Windows.Forms.MessageBox;

namespace ChargeGuard.Application;

/// <summary>
/// Main application context for ChargeGuard.
/// </summary>
public class ChargeGuardApplicationContext : ApplicationContext
{
    private readonly IAppLogger _logger;
    private readonly ChargeGuardSettings _settings;
    private readonly SettingsManager _settingsManager;
    private readonly StartupManager _startupManager;
    private readonly BatteryMonitor _batteryMonitor;
    private readonly ChargingAlertEvaluator _alertEvaluator;
    private readonly NotifyIcon _notifyIcon;
    private readonly ISoundPlayer _soundPlayer;
    private readonly IAlertNotifier _alertNotifier;
    private readonly IAlertNotifier _tooltipNotifier;
    private readonly Timer _reminderCheckTimer;
    private DateTime? _lastAlertTime;

    private const int ReminderCheckIntervalMs = 1000; // Check every second for reminder due times

    public ChargeGuardApplicationContext(
        IAppLogger logger,
        ChargeGuardSettings settings,
        SettingsManager settingsManager,
        StartupManager startupManager,
        BatteryMonitor batteryMonitor,
        ChargingAlertEvaluator alertEvaluator,
        ISoundPlayer soundPlayer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        _startupManager = startupManager ?? throw new ArgumentNullException(nameof(startupManager));
        _batteryMonitor = batteryMonitor ?? throw new ArgumentNullException(nameof(batteryMonitor));
        _alertEvaluator = alertEvaluator ?? throw new ArgumentNullException(nameof(alertEvaluator));
        _soundPlayer = soundPlayer ?? throw new ArgumentNullException(nameof(soundPlayer));

        _notifyIcon = CreateNotifyIcon();
        _alertNotifier = new DialogAlertNotifier(_soundPlayer, _logger);
        _tooltipNotifier = new NotifyIconAlertNotifier(_notifyIcon, _soundPlayer, _logger);

        _reminderCheckTimer = new Timer { Interval = ReminderCheckIntervalMs };
        _reminderCheckTimer.Tick += OnReminderCheckTimerTick;

        _batteryMonitor.BatteryStateChanged += OnBatteryStateChanged;

        _logger.LogInfo("ChargeGuardApplicationContext initialized");
    }

    private NotifyIcon CreateNotifyIcon()
    {
        var notifyIcon = new NotifyIcon
        {
            Text = "ChargeGuard — Initializing",
            Visible = true,
            Icon = SystemIcons.Application
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open ChargeGuard", null, OnOpenSettings);
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Charge to 100% this session", null, OnChargeTo100);
        contextMenu.Items.Add("Snooze reminders for 10 minutes", null, OnSnooze);
        contextMenu.Items.Add("Pause alerts", null, OnPauseAlerts);
        contextMenu.Items.Add("Resume alerts", null, OnResumeAlerts);
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Start with Windows", null, OnToggleStartup);
        contextMenu.Items.Add("View latest log", null, OnViewLog);
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("About", null, OnAbout);
        contextMenu.Items.Add("Exit", null, OnExit);

        notifyIcon.ContextMenuStrip = contextMenu;
        notifyIcon.DoubleClick += OnNotifyIconDoubleClick;

        return notifyIcon;
    }

    /// <summary>
    /// Starts the application.
    /// </summary>
    public void Start()
    {
        _batteryMonitor.Start();
        _reminderCheckTimer.Start();
        UpdateTrayIcon();
        _logger.LogInfo("ChargeGuard started");
    }

    /// <summary>
    /// Stops the application.
    /// </summary>
    public void Stop()
    {
        _reminderCheckTimer.Stop();
        _batteryMonitor.Stop();
        _notifyIcon.Visible = false;
        _logger.LogInfo("ChargeGuard stopped");
    }

    private void OnBatteryStateChanged(object? sender, BatteryStateChangedEventArgs e)
    {
        var decision = _alertEvaluator.EvaluateState(e.CurrentState);
        if (decision != null)
        {
            _lastAlertTime = DateTime.UtcNow;
            _alertNotifier.ShowAlert(decision);
        }

        UpdateTrayIcon();
    }

    private void OnReminderCheckTimerTick(object? sender, EventArgs e)
    {
        var session = _alertEvaluator.CurrentSession;
        if (session != null && session.IsReminderDue())
        {
            // Re-evaluate to trigger the reminder
            var snapshot = _batteryMonitor.GetCurrentBatterySnapshot();
            var decision = _alertEvaluator.EvaluateState(snapshot);
            if (decision != null)
            {
                _lastAlertTime = DateTime.UtcNow;
                _alertNotifier.ShowAlert(decision);
            }
        }
    }

    private void UpdateTrayIcon()
    {
        var snapshot = _batteryMonitor.GetCurrentBatterySnapshot();
        var session = _alertEvaluator.CurrentSession;

        string status;
        if (!snapshot.IsBatteryAvailable)
        {
            status = "battery unavailable";
        }
        else if (!snapshot.IsAcPowerConnected)
        {
            var percentage = snapshot.BatteryPercentage?.ToString() ?? "Unknown";
            status = $"{percentage}% on battery";
        }
        else if (session != null && session.IsTemporaryFullChargeMode)
        {
            var percentage = snapshot.BatteryPercentage?.ToString() ?? "Unknown";
            status = $"{percentage}% charging to 100%";
        }
        else if (session != null && session.TargetAlertSent)
        {
            var percentage = snapshot.BatteryPercentage?.ToString() ?? "Unknown";
            status = $"{percentage}% target reached";
        }
        else if (snapshot.IsCharging)
        {
            var percentage = snapshot.BatteryPercentage?.ToString() ?? "Unknown";
            status = $"{percentage}% charging";
        }
        else
        {
            var percentage = snapshot.BatteryPercentage?.ToString() ?? "Unknown";
            status = $"{percentage}% on AC power";
        }

        _tooltipNotifier.UpdateTooltip(status);
    }

    private void OnNotifyIconDoubleClick(object? sender, EventArgs e)
    {
        OnOpenSettings(sender, e);
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        try
        {
            var settingsForm = new UI.SettingsForm(
                _settings,
                _settingsManager,
                _batteryMonitor,
                _alertEvaluator,
                _startupManager);

            settingsForm.ShowDialog();
            _logger.LogInfo("Settings window closed");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to open settings window", ex);
            MessageBox.Show("Failed to open settings window.", "ChargeGuard", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnChargeTo100(object? sender, EventArgs e)
    {
        _alertEvaluator.EnableTemporaryFullChargeMode();
        UpdateTrayIcon();
        _logger.LogInfo("Temporary 100% mode enabled");
    }

    private void OnSnooze(object? sender, EventArgs e)
    {
        _alertEvaluator.Snooze(TimeSpan.FromMinutes(10));
        _logger.LogInfo("Reminders snoozed for 10 minutes");
    }

    private void OnPauseAlerts(object? sender, EventArgs e)
    {
        _alertEvaluator.PauseAlerts();
        _logger.LogInfo("Alerts paused");
    }

    private void OnResumeAlerts(object? sender, EventArgs e)
    {
        _alertEvaluator.ResumeAlerts();
        _logger.LogInfo("Alerts resumed");
    }

    private void OnToggleStartup(object? sender, EventArgs e)
    {
        var currentStatus = _startupManager.IsStartupEnabled();
        var newStatus = !currentStatus;
        _startupManager.SetStartupEnabled(newStatus);

        _settings.StartWithWindows = newStatus;
        _settingsManager.SaveSettings(_settings);

        _logger.LogInfo($"Startup with Windows {(newStatus ? "enabled" : "disabled")}");
    }

    private void OnViewLog(object? sender, EventArgs e)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ChargeGuard",
                "Logs");

            if (Directory.Exists(logDir))
            {
                var logFiles = Directory.GetFiles(logDir, "ChargeGuard_*.log")
                    .OrderByDescending(f => f)
                    .FirstOrDefault();

                if (logFiles != null)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logFiles,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("No log files found.", "ChargeGuard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Log directory not found.", "ChargeGuard", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to open log file", ex);
            MessageBox.Show("Failed to open log file.", "ChargeGuard", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        try
        {
            var aboutForm = new UI.AboutForm();
            aboutForm.ShowDialog();
            _logger.LogInfo("About window closed");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to open about window", ex);
            MessageBox.Show("Failed to open about window.", "ChargeGuard", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _logger.LogInfo("Exit requested by user");
        WinFormsApplication.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _reminderCheckTimer.Dispose();
            _batteryMonitor.Dispose();
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
