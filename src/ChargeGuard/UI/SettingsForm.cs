using System.Windows.Forms;
using ChargeGuard.Battery;
using ChargeGuard.Charging;
using ChargeGuard.Settings;
using Timer = System.Windows.Forms.Timer;

namespace ChargeGuard.UI;

/// <summary>
/// Settings form for ChargeGuard.
/// </summary>
public partial class SettingsForm : Form
{
    private readonly ChargeGuardSettings _settings;
    private readonly SettingsManager _settingsManager;
    private readonly BatteryMonitor _batteryMonitor;
    private readonly ChargingAlertEvaluator _alertEvaluator;
    private readonly StartupManager _startupManager;
    private readonly Timer _refreshTimer;

    // UI Controls
    private Label _currentPercentageLabel = null!;
    private Label _currentPowerSourceLabel = null!;
    private Label _chargingStatusLabel = null!;
    private Label _activeTargetLabel = null!;
    private Label _temporaryModeLabel = null!;
    private Label _lastAlertLabel = null!;
    private Label _chargerConnectedLabel = null!;

    private NumericUpDown _targetPercentageInput = null!;
    private CheckBox _advanceWarningCheckBox = null!;
    private NumericUpDown _advanceWarningPercentageInput = null!;
    private CheckBox _repeatedRemindersCheckBox = null!;
    private NumericUpDown _firstReminderDelayInput = null!;
    private NumericUpDown _repeatedReminderIntervalInput = null!;
    private NumericUpDown _escalationPercentageInput = null!;
    private CheckBox _soundEnabledCheckBox = null!;
    private CheckBox _startWithWindowsCheckBox = null!;
    private CheckBox _startMinimizedCheckBox = null!;

    public SettingsForm(
        ChargeGuardSettings settings,
        SettingsManager settingsManager,
        BatteryMonitor batteryMonitor,
        ChargingAlertEvaluator alertEvaluator,
        StartupManager startupManager)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        _batteryMonitor = batteryMonitor ?? throw new ArgumentNullException(nameof(batteryMonitor));
        _alertEvaluator = alertEvaluator ?? throw new ArgumentNullException(nameof(alertEvaluator));
        _startupManager = startupManager ?? throw new ArgumentNullException(nameof(startupManager));

        _refreshTimer = new Timer { Interval = 1000 };
        _refreshTimer.Tick += OnRefreshTimerTick;

        InitializeComponent();
        LoadSettings();
        RefreshBatteryStatus();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form properties - modern dark theme
        this.Text = "ChargeGuard Settings";
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MinimumSize = new Size(600, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.ClientSize = new Size(650, 750);
        this.BackColor = Color.FromArgb(245, 245, 245);
        this.Font = new Font("Segoe UI", 9);

        // Create header
        var headerPanel = CreateHeader();
        headerPanel.Location = new Point(0, 0);
        headerPanel.Size = new Size(this.ClientSize.Width, 80);

        // Create status section
        var statusGroup = CreateStatusSection();
        statusGroup.Location = new Point(20, 100);
        statusGroup.Size = new Size(290, 180);

        // Create settings section
        var settingsGroup = CreateSettingsSection();
        settingsGroup.Location = new Point(330, 100);
        settingsGroup.Size = new Size(300, 550);

        // Create buttons
        var buttonPanel = CreateButtonPanel();
        buttonPanel.Location = new Point(20, 680);
        buttonPanel.Size = new Size(610, 50);

        // Add controls
        this.Controls.Add(headerPanel);
        this.Controls.Add(statusGroup);
        this.Controls.Add(settingsGroup);
        this.Controls.Add(buttonPanel);

        this.ResumeLayout(false);
    }

    private Panel CreateHeader()
    {
        var panel = new Panel
        {
            BackColor = Color.FromArgb(0, 120, 215),
            Dock = DockStyle.Top
        };

        var titleLabel = new Label
        {
            Text = "⚡ ChargeGuard",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 20),
            AutoSize = true
        };

        var subtitleLabel = new Label
        {
            Text = "Battery Charging Alert Utility",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(200, 200, 200),
            Location = new Point(20, 50),
            AutoSize = true
        };

        panel.Controls.Add(titleLabel);
        panel.Controls.Add(subtitleLabel);

        return panel;
    }

    private Panel CreateButtonPanel()
    {
        var panel = new Panel
        {
            BackColor = Color.Transparent
        };

        var saveButton = new Button
        {
            Text = "💾 Save Settings",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(380, 10),
            Size = new Size(140, 35),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        saveButton.FlatAppearance.BorderSize = 0;
        saveButton.Click += OnSaveClick;

        var cancelButton = new Button
        {
            Text = "✕ Cancel",
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(530, 10),
            Size = new Size(80, 35),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.FlatAppearance.BorderSize = 0;

        panel.Controls.Add(saveButton);
        panel.Controls.Add(cancelButton);

        return panel;
    }

    private GroupBox CreateStatusSection()
    {
        var group = new GroupBox
        {
            Text = "📊 Current Status",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        _currentPercentageLabel = CreateModernLabel("Battery: --%", Color.FromArgb(0, 120, 215));
        _currentPowerSourceLabel = CreateModernLabel("Power Source: --", Color.FromArgb(0, 0, 0));
        _chargingStatusLabel = CreateModernLabel("Charging: --", Color.FromArgb(0, 0, 0));
        _activeTargetLabel = CreateModernLabel("Active Target: --%", Color.FromArgb(0, 120, 215));
        _temporaryModeLabel = CreateModernLabel("Temporary 100% Mode: --", Color.FromArgb(0, 0, 0));
        _lastAlertLabel = CreateModernLabel("Last Alert: --", Color.FromArgb(0, 0, 0));
        _chargerConnectedLabel = CreateModernLabel("Charger Connected: --", Color.FromArgb(0, 0, 0));

        group.Controls.Add(_currentPercentageLabel);
        group.Controls.Add(_currentPowerSourceLabel);
        group.Controls.Add(_chargingStatusLabel);
        group.Controls.Add(_activeTargetLabel);
        group.Controls.Add(_temporaryModeLabel);
        group.Controls.Add(_lastAlertLabel);
        group.Controls.Add(_chargerConnectedLabel);

        // Layout - more spaced out
        _currentPercentageLabel.Location = new Point(15, 25);
        _currentPowerSourceLabel.Location = new Point(15, 55);
        _chargingStatusLabel.Location = new Point(15, 85);
        _activeTargetLabel.Location = new Point(15, 115);
        _temporaryModeLabel.Location = new Point(15, 145);
        _lastAlertLabel.Location = new Point(15, 175);
        _chargerConnectedLabel.Location = new Point(15, 205);

        return group;
    }

    private GroupBox CreateSettingsSection()
    {
        var group = new GroupBox
        {
            Text = "⚙️ Settings",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        // Normal target percentage
        var targetLabel = new Label { 
            Text = "Normal Target (%):", 
            Location = new Point(15, 25), 
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };
        _targetPercentageInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100,
            Value = _settings.NormalTargetPercentage,
            Location = new Point(160, 23),
            Size = new Size(70, 25),
            Font = new Font("Segoe UI", 9)
        };

        // Advance warning
        _advanceWarningCheckBox = new CheckBox
        {
            Text = "Enable Advance Warning",
            Location = new Point(15, 60),
            Checked = _settings.AdvanceWarningEnabled,
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };

        var advanceWarningLabel = new Label { 
            Text = "Advance Warning (%):", 
            Location = new Point(35, 85), 
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };
        _advanceWarningPercentageInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100,
            Value = _settings.AdvanceWarningPercentage,
            Location = new Point(185, 83),
            Size = new Size(70, 25),
            Font = new Font("Segoe UI", 9)
        };

        // Repeated reminders
        _repeatedRemindersCheckBox = new CheckBox
        {
            Text = "Enable Repeated Reminders",
            Location = new Point(15, 120),
            Checked = _settings.RepeatedRemindersEnabled,
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };

        var firstReminderLabel = new Label { 
            Text = "First Reminder (min):", 
            Location = new Point(35, 145), 
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };
        _firstReminderDelayInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 60,
            Value = (decimal)_settings.FirstReminderDelay.TotalMinutes,
            Location = new Point(185, 143),
            Size = new Size(70, 25),
            Font = new Font("Segoe UI", 9)
        };

        var reminderIntervalLabel = new Label { 
            Text = "Reminder Interval (min):", 
            Location = new Point(35, 175), 
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };
        _repeatedReminderIntervalInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 60,
            Value = (decimal)_settings.RepeatedReminderInterval.TotalMinutes,
            Location = new Point(215, 173),
            Size = new Size(70, 25),
            Font = new Font("Segoe UI", 9)
        };

        // Escalation
        var escalationLabel = new Label { 
            Text = "Escalation (%):", 
            Location = new Point(15, 205), 
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };
        _escalationPercentageInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100,
            Value = _settings.EscalationPercentage,
            Location = new Point(160, 203),
            Size = new Size(70, 25),
            Font = new Font("Segoe UI", 9)
        };

        // Sound
        _soundEnabledCheckBox = new CheckBox
        {
            Text = "Enable Sound Notifications",
            Location = new Point(15, 240),
            Checked = _settings.SoundEnabled,
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };

        // Startup
        _startWithWindowsCheckBox = new CheckBox
        {
            Text = "Start with Windows",
            Location = new Point(15, 275),
            Checked = _settings.StartWithWindows,
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };

        // Start minimized
        _startMinimizedCheckBox = new CheckBox
        {
            Text = "Start Minimized",
            Location = new Point(15, 310),
            Checked = _settings.StartMinimized,
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };

        // Add controls
        group.Controls.Add(targetLabel);
        group.Controls.Add(_targetPercentageInput);
        group.Controls.Add(_advanceWarningCheckBox);
        group.Controls.Add(advanceWarningLabel);
        group.Controls.Add(_advanceWarningPercentageInput);
        group.Controls.Add(_repeatedRemindersCheckBox);
        group.Controls.Add(firstReminderLabel);
        group.Controls.Add(_firstReminderDelayInput);
        group.Controls.Add(reminderIntervalLabel);
        group.Controls.Add(_repeatedReminderIntervalInput);
        group.Controls.Add(escalationLabel);
        group.Controls.Add(_escalationPercentageInput);
        group.Controls.Add(_soundEnabledCheckBox);
        group.Controls.Add(_startWithWindowsCheckBox);
        group.Controls.Add(_startMinimizedCheckBox);

        return group;
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9),
            AutoSize = true
        };
    }

    private static Label CreateModernLabel(string text, Color color)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = color,
            AutoSize = true
        };
    }

    private void LoadSettings()
    {
        _targetPercentageInput.Value = _settings.NormalTargetPercentage;
        _advanceWarningCheckBox.Checked = _settings.AdvanceWarningEnabled;
        _advanceWarningPercentageInput.Value = _settings.AdvanceWarningPercentage;
        _repeatedRemindersCheckBox.Checked = _settings.RepeatedRemindersEnabled;
        _firstReminderDelayInput.Value = (decimal)_settings.FirstReminderDelay.TotalMinutes;
        _repeatedReminderIntervalInput.Value = (decimal)_settings.RepeatedReminderInterval.TotalMinutes;
        _escalationPercentageInput.Value = _settings.EscalationPercentage;
        _soundEnabledCheckBox.Checked = _settings.SoundEnabled;
        _startWithWindowsCheckBox.Checked = _settings.StartWithWindows;
        _startMinimizedCheckBox.Checked = _settings.StartMinimized;
    }

    private void RefreshBatteryStatus()
    {
        var snapshot = _batteryMonitor.GetCurrentBatterySnapshot();
        var session = _alertEvaluator.CurrentSession;

        _currentPercentageLabel.Text = $"Battery: {snapshot.BatteryPercentage?.ToString() ?? "Unknown"}%";
        _currentPowerSourceLabel.Text = $"Power Source: {(snapshot.IsAcPowerConnected ? "AC" : "Battery")}";
        _chargingStatusLabel.Text = $"Charging: {(snapshot.IsCharging ? "Yes" : "No")}";
        _activeTargetLabel.Text = $"Active Target: {session?.ActiveTargetPercentage ?? _settings.NormalTargetPercentage}%";
        _temporaryModeLabel.Text = $"Temporary 100% Mode: {(session?.IsTemporaryFullChargeMode == true ? "Yes" : "No")}";
        _lastAlertLabel.Text = "Last Alert: --";
        _chargerConnectedLabel.Text = $"Charger Connected: {(snapshot.IsAcPowerConnected ? "Yes" : "No")}";
    }

    private void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        RefreshBatteryStatus();
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        // Update settings from UI
        _settings.NormalTargetPercentage = (int)_targetPercentageInput.Value;
        _settings.AdvanceWarningEnabled = _advanceWarningCheckBox.Checked;
        _settings.AdvanceWarningPercentage = (int)_advanceWarningPercentageInput.Value;
        _settings.RepeatedRemindersEnabled = _repeatedRemindersCheckBox.Checked;
        _settings.FirstReminderDelay = TimeSpan.FromMinutes((double)_firstReminderDelayInput.Value);
        _settings.RepeatedReminderInterval = TimeSpan.FromMinutes((double)_repeatedReminderIntervalInput.Value);
        _settings.EscalationPercentage = (int)_escalationPercentageInput.Value;
        _settings.SoundEnabled = _soundEnabledCheckBox.Checked;
        _settings.StartWithWindows = _startWithWindowsCheckBox.Checked;
        _settings.StartMinimized = _startMinimizedCheckBox.Checked;

        // Validate and normalize
        _settings.ValidateAndNormalize();

        // Save to file
        _settingsManager.SaveSettings(_settings);

        // Update startup registration
        _startupManager.SetStartupEnabled(_settings.StartWithWindows);

        // Update alert evaluator
        _alertEvaluator.UpdateSettings(_settings);

        MessageBox.Show("Settings saved successfully.", "ChargeGuard", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _refreshTimer.Start();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _refreshTimer.Stop();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
