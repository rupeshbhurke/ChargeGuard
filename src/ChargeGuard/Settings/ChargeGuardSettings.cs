namespace ChargeGuard.Settings;

/// <summary>
/// Application settings for ChargeGuard.
/// </summary>
public class ChargeGuardSettings
{
    private int _normalTargetPercentage = 80;
    private int _advanceWarningPercentage = 75;
    private int _escalationPercentage = 90;
    private TimeSpan _firstReminderDelay = TimeSpan.FromMinutes(5);
    private TimeSpan _repeatedReminderInterval = TimeSpan.FromMinutes(10);
    private TimeSpan _notificationTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the normal charging target percentage (1-100).
    /// </summary>
    public int NormalTargetPercentage
    {
        get => _normalTargetPercentage;
        set => _normalTargetPercentage = ClampPercentage(value);
    }

    /// <summary>
    /// Gets or sets whether advance warning is enabled.
    /// </summary>
    public bool AdvanceWarningEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the advance warning percentage (1-100, must be less than NormalTargetPercentage).
    /// </summary>
    public int AdvanceWarningPercentage
    {
        get => _advanceWarningPercentage;
        set => _advanceWarningPercentage = ClampPercentage(value);
    }

    /// <summary>
    /// Gets or sets whether repeated reminders are enabled.
    /// </summary>
    public bool RepeatedRemindersEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the delay before the first reminder after target is reached.
    /// </summary>
    public TimeSpan FirstReminderDelay
    {
        get => _firstReminderDelay;
        set => _firstReminderDelay = ClampReminderInterval(value);
    }

    /// <summary>
    /// Gets or sets the interval between repeated reminders.
    /// </summary>
    public TimeSpan RepeatedReminderInterval
    {
        get => _repeatedReminderInterval;
        set => _repeatedReminderInterval = ClampReminderInterval(value);
    }

    /// <summary>
    /// Gets or sets the escalation percentage (1-100, must be >= NormalTargetPercentage).
    /// </summary>
    public int EscalationPercentage
    {
        get => _escalationPercentage;
        set => _escalationPercentage = ClampPercentage(value);
    }

    /// <summary>
    /// Gets or sets whether sound notifications are enabled.
    /// </summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the application starts with Windows.
    /// </summary>
    public bool StartWithWindows { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the application starts minimized to the notification area.
    /// </summary>
    public bool StartMinimized { get; set; } = true;

    /// <summary>
    /// Gets or sets the temporary full-charge target percentage (default 100%).
    /// </summary>
    public int TemporaryFullChargeTarget { get; set; } = 100;

    /// <summary>
    /// Gets or sets the notification timeout duration (default 10 seconds).
    /// </summary>
    public TimeSpan NotificationTimeout
    {
        get => _notificationTimeout;
        set => _notificationTimeout = ClampNotificationTimeout(value);
    }

    /// <summary>
    /// Validates and normalizes the settings to ensure consistency.
    /// </summary>
    public void ValidateAndNormalize()
    {
        // Ensure advance warning is less than normal target
        if (_advanceWarningPercentage >= _normalTargetPercentage)
        {
            _advanceWarningPercentage = Math.Max(1, _normalTargetPercentage - 5);
        }

        // Ensure escalation is greater than or equal to normal target
        if (_escalationPercentage < _normalTargetPercentage)
        {
            _escalationPercentage = _normalTargetPercentage;
        }

        // Clamp all percentages to valid range
        _normalTargetPercentage = ClampPercentage(_normalTargetPercentage);
        _advanceWarningPercentage = ClampPercentage(_advanceWarningPercentage);
        _escalationPercentage = ClampPercentage(_escalationPercentage);
        TemporaryFullChargeTarget = ClampPercentage(TemporaryFullChargeTarget);

        // Clamp reminder intervals to reasonable limits
        _firstReminderDelay = ClampReminderInterval(_firstReminderDelay);
        _repeatedReminderInterval = ClampReminderInterval(_repeatedReminderInterval);

        // Clamp notification timeout to reasonable limits
        _notificationTimeout = ClampNotificationTimeout(_notificationTimeout);
    }

    private static int ClampPercentage(int value)
    {
        return Math.Clamp(value, 1, 100);
    }

    private static TimeSpan ClampReminderInterval(TimeSpan value)
    {
        // Minimum 1 minute, maximum 1 hour
        var totalMinutes = value.TotalMinutes;
        totalMinutes = Math.Clamp(totalMinutes, 1, 60);
        return TimeSpan.FromMinutes(totalMinutes);
    }

    private static TimeSpan ClampNotificationTimeout(TimeSpan value)
    {
        // Minimum 3 seconds, maximum 60 seconds
        var totalSeconds = value.TotalSeconds;
        totalSeconds = Math.Clamp(totalSeconds, 3, 60);
        return TimeSpan.FromSeconds(totalSeconds);
    }

    /// <summary>
    /// Creates a copy of the current settings.
    /// </summary>
    public ChargeGuardSettings Clone()
    {
        return new ChargeGuardSettings
        {
            NormalTargetPercentage = NormalTargetPercentage,
            AdvanceWarningEnabled = AdvanceWarningEnabled,
            AdvanceWarningPercentage = AdvanceWarningPercentage,
            RepeatedRemindersEnabled = RepeatedRemindersEnabled,
            FirstReminderDelay = FirstReminderDelay,
            RepeatedReminderInterval = RepeatedReminderInterval,
            EscalationPercentage = EscalationPercentage,
            SoundEnabled = SoundEnabled,
            StartWithWindows = StartWithWindows,
            StartMinimized = StartMinimized,
            TemporaryFullChargeTarget = TemporaryFullChargeTarget,
            NotificationTimeout = NotificationTimeout
        };
    }
}
