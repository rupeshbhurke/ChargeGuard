using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ChargeGuard.Analytics;
using ChargeGuard.Battery;
using ChargeGuard.Charging;
using ChargeGuard.Logging;
using ChargeGuard.Notifications;
using ChargeGuard.Settings;
using ChargeGuard.UI;
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
    private readonly BatteryDatabase _batteryDatabase;
    private readonly BatteryAnalyticsService _analyticsService;
    private readonly BatteryAnalyticsQueries _analyticsQueries;
    private DateTime? _lastAlertTime;
    private readonly Dictionary<int, Icon> _iconCache = new();
    private Icon? _baseIcon;
    private int _lastDisplayedPercentage = -1;

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

        // Initialize analytics
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChargeGuard",
            "battery_analytics.db"
        );
        _batteryDatabase = new BatteryDatabase(dbPath);
        _batteryDatabase.Initialize();
        _analyticsService = new BatteryAnalyticsService(_batteryDatabase, _logger, _settings);
        _analyticsQueries = new BatteryAnalyticsQueries(_batteryDatabase);

        _notifyIcon = CreateNotifyIcon();
        _alertNotifier = new DialogAlertNotifier(_soundPlayer, _logger);
        _tooltipNotifier = new NotifyIconAlertNotifier(_notifyIcon, _soundPlayer, _logger, _settings);

        _reminderCheckTimer = new Timer { Interval = ReminderCheckIntervalMs };
        _reminderCheckTimer.Tick += OnReminderCheckTimerTick;

        _batteryMonitor.BatteryStateChanged += OnBatteryStateChanged;

        _logger.LogInfo("ChargeGuardApplicationContext initialized");
    }

    private NotifyIcon CreateNotifyIcon()
    {
        // Try to load icon from the application's embedded icon
        try
        {
            _baseIcon = Icon.ExtractAssociatedIcon(WinFormsApplication.ExecutablePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to extract icon from executable: {ex.Message}");
            
            // Fallback to loading from file
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChargeGuard.ico");
            if (File.Exists(iconPath))
            {
                try
                {
                    _baseIcon = new Icon(iconPath);
                }
                catch (Exception fileEx)
                {
                    _logger.LogWarning($"Failed to load icon from {iconPath}: {fileEx.Message}");
                }
            }
        }
        
        var notifyIcon = new NotifyIcon
        {
            Text = "ChargeGuard — Initializing",
            Visible = true,
            Icon = _baseIcon ?? SystemIcons.Application
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open ChargeGuard", null, OnOpenSettings);
        contextMenu.Items.Add("📊 Battery Analytics", null, OnOpenAnalytics);
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
        _analyticsService.Start();
        _reminderCheckTimer.Start();
        
        // Simple startup check: if battery is at or above target and charging, show alert immediately
        var initialSnapshot = _batteryMonitor.GetCurrentBatterySnapshot();
        if (initialSnapshot.IsAcPowerConnected && 
            initialSnapshot.IsCharging && 
            initialSnapshot.BatteryPercentage.HasValue &&
            initialSnapshot.BatteryPercentage.Value >= _settings.NormalTargetPercentage)
        {
            _logger.LogInfo($"Startup: Battery at {initialSnapshot.BatteryPercentage}% (target: {_settings.NormalTargetPercentage}%), showing alert");
            
            var alertDecision = new ChargingAlertDecision(
                ChargingAlertType.Target,
                $"Charging target reached\nBattery is at {initialSnapshot.BatteryPercentage}%. You can disconnect the charger.",
                initialSnapshot.BatteryPercentage.Value,
                _settings.NormalTargetPercentage,
                playSound: _settings.SoundEnabled);
            
            _lastAlertTime = DateTime.UtcNow;
            _alertNotifier.ShowAlert(alertDecision);
        }
        
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
        // Record battery reading for analytics
        _analyticsService.RecordReading(e.CurrentState);

        // Simple check: if battery is at or above target and charging, show alert
        if (e.CurrentState.IsAcPowerConnected &&
            e.CurrentState.IsCharging &&
            e.CurrentState.BatteryPercentage.HasValue &&
            e.CurrentState.BatteryPercentage.Value >= _settings.NormalTargetPercentage)
        {
            _logger.LogInfo($"Battery at {e.CurrentState.BatteryPercentage}% (target: {_settings.NormalTargetPercentage}%), showing alert");

            var alertDecision = new ChargingAlertDecision(
                ChargingAlertType.Target,
                $"Charging target reached\nBattery is at {e.CurrentState.BatteryPercentage}%. You can disconnect the charger.",
                e.CurrentState.BatteryPercentage.Value,
                _settings.NormalTargetPercentage,
                playSound: _settings.SoundEnabled);

            _lastAlertTime = DateTime.UtcNow;
            _alertNotifier.ShowAlert(alertDecision);
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
        int? percentage = snapshot.BatteryPercentage;
        
        if (!snapshot.IsBatteryAvailable)
        {
            status = "battery unavailable";
            percentage = null;
        }
        else if (!snapshot.IsAcPowerConnected)
        {
            status = $"{percentage}% on battery";
        }
        else if (session != null && session.IsTemporaryFullChargeMode)
        {
            status = $"{percentage}% charging to 100%";
        }
        else if (session != null && session.TargetAlertSent)
        {
            status = $"{percentage}% target reached";
        }
        else if (snapshot.IsCharging)
        {
            status = $"{percentage}% charging";
        }
        else
        {
            status = $"{percentage}% on AC power";
        }

        _tooltipNotifier.UpdateTooltip(status);

        // Update icon with percentage if available and changed
        if (percentage.HasValue && percentage != _lastDisplayedPercentage)
        {
            var iconWithPercentage = GetIconWithPercentage(percentage.Value);
            if (iconWithPercentage != null)
            {
                _notifyIcon.Icon = iconWithPercentage;
                _lastDisplayedPercentage = percentage.Value;
            }
        }
        else if (!percentage.HasValue && _lastDisplayedPercentage != -1)
        {
            // Reset to base icon when percentage is unavailable
            if (_baseIcon != null)
            {
                _notifyIcon.Icon = _baseIcon;
                _lastDisplayedPercentage = -1;
            }
        }
    }

    private Icon? GetIconWithPercentage(int percentage)
    {
        if (_baseIcon == null)
            return null;

        // Check cache first
        if (_iconCache.TryGetValue(percentage, out var cachedIcon))
            return cachedIcon;

        try
        {
            // Use standard Windows 11 tray icon size
            const int iconSize = 48; // Standard tray icon size
            using var bitmap = new Bitmap(iconSize, iconSize);
            using var graphics = Graphics.FromImage(bitmap);
            
            // Draw base icon scaled to fill the bitmap
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawImage(_baseIcon.ToBitmap(), 0, 0, iconSize, iconSize);
            
            // Configure text rendering
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            
            // Calculate text size and position - fill most of the icon
            string text = percentage.ToString();
            var fontSize = (float)Math.Max(20, iconSize / 2.2);
            using var font = new Font(new FontFamily("Arial"), fontSize, FontStyle.Bold);
            
            var textSize = graphics.MeasureString(text, font);
            var x = (iconSize - textSize.Width) / 2;
            var y = (iconSize - textSize.Height) / 2;
            
            // Draw semi-transparent background with minimal padding
            var padding = 3;
            var bgRect = new RectangleF(x - padding, y - padding, textSize.Width + padding * 2, textSize.Height + padding * 2);
            using var bgBrush = new SolidBrush(Color.FromArgb(240, Color.Black));
            graphics.FillRectangle(bgBrush, bgRect);
            
            // Draw percentage text
            using var textBrush = new SolidBrush(Color.White);
            graphics.DrawString(text, font, textBrush, x, y);
            
            // Create icon from bitmap
            var icon = Icon.FromHandle(bitmap.GetHicon());
            
            // Cache the icon
            _iconCache[percentage] = icon;
            
            return icon;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to create icon with percentage {percentage}: {ex.Message}");
            return _baseIcon;
        }
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

    private void OnOpenAnalytics(object? sender, EventArgs e)
    {
        try
        {
            var analyticsWindow = new AnalyticsWindow(_analyticsQueries, _logger);
            analyticsWindow.ShowDialog();
            _logger.LogInfo("Analytics window closed");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to open analytics window", ex);
            MessageBox.Show("Failed to open analytics window.", "ChargeGuard", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            
            // Dispose cached icons
            foreach (var icon in _iconCache.Values)
            {
                icon?.Dispose();
            }
            _iconCache.Clear();
            
            _baseIcon?.Dispose();
        }

        base.Dispose(disposing);
    }
}
