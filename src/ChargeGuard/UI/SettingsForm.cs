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

        // Form properties
        this.Text = "ChargeGuard Settings";
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MinimumSize = new Size(500, 600);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.ClientSize = new Size(550, 700);
        this.BackColor = Color.White;

        // Create status section
        var statusGroup = CreateStatusSection();
        statusGroup.Location = new Point(12, 12);
        statusGroup.Size = new Size(510, 120);

        // Create settings section
        var settingsGroup = CreateSettingsSection();
        settingsGroup.Location = new Point(12, 140);
        settingsGroup.Size = new Size(510, 450);

        // Create buttons
        var saveButton = new Button
        {
            Text = "Save",
            DialogResult = DialogResult.OK,
            Location = new Point(350, 610),
            Size = new Size(80, 30),
            UseVisualStyleBackColor = true
        };
        saveButton.Click += OnSaveClick;

        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(440, 610),
            Size = new Size(80, 30),
            UseVisualStyleBackColor = true
        };

        // Add controls
        this.Controls.Add(statusGroup);
        this.Controls.Add(settingsGroup);
        this.Controls.Add(saveButton);
        this.Controls.Add(cancelButton);

        this.ResumeLayout(false);
    }

    private GroupBox CreateStatusSection()
    {
        var group = new GroupBox
        {
            Text = "Current Status",
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        _currentPercentageLabel = CreateLabel("Battery: --%");
        _currentPowerSourceLabel = CreateLabel("Power Source: --");
        _chargingStatusLabel = CreateLabel("Charging: --");
        _activeTargetLabel = CreateLabel("Active Target: --%");
        _temporaryModeLabel = CreateLabel("Temporary 100% Mode: --");
        _lastAlertLabel = CreateLabel("Last Alert: --");
        _chargerConnectedLabel = CreateLabel("Charger Connected: --");

        group.Controls.Add(_currentPercentageLabel);
        group.Controls.Add(_currentPowerSourceLabel);
        group.Controls.Add(_chargingStatusLabel);
        group.Controls.Add(_activeTargetLabel);
        group.Controls.Add(_temporaryModeLabel);
        group.Controls.Add(_lastAlertLabel);
        group.Controls.Add(_chargerConnectedLabel);

        // Layout
        _currentPercentageLabel.Location = new Point(10, 20);
        _currentPowerSourceLabel.Location = new Point(10, 40);
        _chargingStatusLabel.Location = new Point(10, 60);
        _activeTargetLabel.Location = new Point(270, 20);
        _temporaryModeLabel.Location = new Point(270, 40);
        _lastAlertLabel.Location = new Point(270, 60);
        _chargerConnectedLabel.Location = new Point(10, 80);

        return group;
    }

    private GroupBox CreateSettingsSection()
    {
        var group = new GroupBox
        {
            Text = "Settings",
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        // Normal target percentage
        var targetLabel = new Label { Text = "Normal Target (%):", Location = new Point(10, 20), AutoSize = true };
        _targetPercentageInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100,
            Value = _settings.NormalTargetPercentage,
            Location = new Point(150, 18),
            Size = new Size(60, 23)
        };

        // Advance warning
        _advanceWarningCheckBox = new CheckBox
        {
            Text = "Enable Advance Warning",
            Location = new Point(10, 50),
            Checked = _settings.AdvanceWarningEnabled,
            AutoSize = true
        };

        var advanceWarningLabel = new Label { Text = "Advance Warning (%):", Location = new Point(30, 75), AutoSize = true };
        _advanceWarningPercentageInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100,
            Value = _settings.AdvanceWarningPercentage,
            Location = new Point(170, 73),
            Size = new Size(60, 23)
        };

        // Repeated reminders
        _repeatedRemindersCheckBox = new CheckBox
        {
            Text = "Enable Repeated Reminders",
            Location = new Point(10, 105),
            Checked = _settings.RepeatedRemindersEnabled,
            AutoSize = true
        };

        var firstReminderLabel = new Label { Text = "First Reminder (min):", Location = new Point(30, 130), AutoSize = true };
        _firstReminderDelayInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 60,
            Value = (decimal)_settings.FirstReminderDelay.TotalMinutes,
            Location = new Point(170, 128),
            Size = new Size(60, 23)
        };

        var reminderIntervalLabel = new Label { Text = "Reminder Interval (min):", Location = new Point(30, 155), AutoSize = true };
        _repeatedReminderIntervalInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 60,
            Value = (decimal)_settings.RepeatedReminderInterval.TotalMinutes,
            Location = new Point(200, 153),
            Size = new Size(60, 23)
        };

        // Escalation
        var escalationLabel = new Label { Text = "Escalation (%):", Location = new Point(10, 185), AutoSize = true };
        _escalationPercentageInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100,
            Value = _settings.EscalationPercentage,
            Location = new Point(150, 183),
            Size = new Size(60, 23)
        };

        // Sound
        _soundEnabledCheckBox = new CheckBox
        {
            Text = "Enable Sound",
            Location = new Point(10, 215),
            Checked = _settings.SoundEnabled,
            AutoSize = true
        };

        // Startup
        _startWithWindowsCheckBox = new CheckBox
        {
            Text = "Start with Windows",
            Location = new Point(10, 245),
            Checked = _settings.StartWithWindows,
            AutoSize = true
        };

        _startMinimizedCheckBox = new CheckBox
        {
            Text = "Start Minimized",
            Location = new Point(10, 270),
            Checked = _settings.StartMinimized,
            AutoSize = true
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
