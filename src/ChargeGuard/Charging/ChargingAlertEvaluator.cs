using ChargeGuard.Battery;
using ChargeGuard.Settings;

namespace ChargeGuard.Charging;

/// <summary>
/// Evaluates battery state and determines when to show charging alerts.
/// </summary>
public class ChargingAlertEvaluator
{
    private readonly ChargeGuardSettings _settings;
    private readonly IClock _clock;
    private ChargingSession? _currentSession;
    private BatterySnapshot? _lastSnapshot;

    public ChargingAlertEvaluator(ChargeGuardSettings settings, IClock? clock = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _clock = clock ?? new SystemClock();
    }

    /// <summary>
    /// Gets the current charging session, if one exists.
    /// </summary>
    public ChargingSession? CurrentSession => _currentSession;

    /// <summary>
    /// Evaluates the battery state and returns an alert decision if needed.
    /// </summary>
    public ChargingAlertDecision? EvaluateState(BatterySnapshot snapshot)
    {
        if (snapshot == null)
            return null;

        var previousSnapshot = _lastSnapshot;
        _lastSnapshot = snapshot;

        // Handle systems without battery
        if (!snapshot.IsBatteryAvailable)
        {
            EndSession();
            return null;
        }

        // Handle unknown battery percentage
        if (!snapshot.BatteryPercentage.HasValue)
        {
            return null;
        }

        var percentage = snapshot.BatteryPercentage.Value;

        // Detect power source changes
        if (previousSnapshot != null && previousSnapshot.IsAcPowerConnected != snapshot.IsAcPowerConnected)
        {
            if (snapshot.IsAcPowerConnected)
            {
                // Charger connected - start new session
                StartSession(percentage);
            }
            else
            {
                // Charger disconnected - end session
                EndSession();
                return null;
            }
        }

        // Ensure session exists if charger is connected
        if (snapshot.IsAcPowerConnected && _currentSession == null)
        {
            StartSession(percentage);
        }

        // End session if charger disconnected
        if (!snapshot.IsAcPowerConnected && _currentSession != null)
        {
            EndSession();
            return null;
        }

        // No session = no alerts
        if (_currentSession == null)
            return null;

        // Check if alerts are paused
        if (_currentSession.AreAlertsPaused)
            return null;

        // Check if snoozed
        if (_currentSession.IsSnoozed())
            return null;

        // Store previous percentage for crossing detection
        var previousPercentage = _currentSession.LastBatteryPercentage;

        // Evaluate alerts
        var alert = EvaluateAlerts(percentage, previousPercentage, snapshot.IsCharging);

        // Update last percentage in session after evaluation
        _currentSession.LastBatteryPercentage = percentage;

        return alert;
    }

    private void StartSession(int currentPercentage)
    {
        var target = _settings.NormalTargetPercentage;
        _currentSession = new ChargingSession(target, _clock);
        _currentSession.LastBatteryPercentage = currentPercentage;

        // If already at or above target, mark target as sent to avoid alert on startup
        if (currentPercentage >= target)
        {
            _currentSession.TargetAlertSent = true;
        }
    }

    private void EndSession()
    {
        if (_currentSession != null)
        {
            _currentSession.ClearAlertStates();
            _currentSession = null;
        }
    }

    private ChargingAlertDecision? EvaluateAlerts(int percentage, int? previousPercentage, bool isCharging)
    {
        if (_currentSession == null)
            return null;

        var session = _currentSession;
        var target = session.ActiveTargetPercentage;
        var previous = previousPercentage ?? percentage;

        // Advance warning
        if (_settings.AdvanceWarningEnabled && !session.AdvanceWarningSent)
        {
            var advanceThreshold = _settings.AdvanceWarningPercentage;

            // Trigger when crossing threshold from below
            if (previous < advanceThreshold && percentage >= advanceThreshold)
            {
                session.AdvanceWarningSent = true;
                return new ChargingAlertDecision(
                    ChargingAlertType.AdvanceWarning,
                    $"Battery is at {percentage}% and approaching the {target}% charging target.",
                    percentage,
                    target,
                    playSound: false);
            }
        }

        // Target alert
        if (!session.TargetAlertSent)
        {
            // Trigger when crossing target from below
            if (previous < target && percentage >= target)
            {
                session.TargetAlertSent = true;
                ScheduleFirstReminder(session);

                return new ChargingAlertDecision(
                    ChargingAlertType.Target,
                    $"Charging target reached\nBattery is at {percentage}%. You can disconnect the charger.",
                    percentage,
                    target,
                    playSound: _settings.SoundEnabled);
            }
        }

        // Escalation alert
        if (session.TargetAlertSent && !session.EscalationAlertSent)
        {
            var escalationThreshold = _settings.EscalationPercentage;

            // Trigger when crossing escalation threshold from below
            if (previous < escalationThreshold && percentage >= escalationThreshold)
            {
                session.EscalationAlertSent = true;
                // Cancel reminders after escalation
                session.NextReminderDue = null;

                return new ChargingAlertDecision(
                    ChargingAlertType.Escalation,
                    $"Battery has reached {percentage}% and remains connected. Your configured target was {target}%.",
                    percentage,
                    target,
                    playSound: _settings.SoundEnabled);
            }
        }

        // Repeated reminders
        if (_settings.RepeatedRemindersEnabled && session.TargetAlertSent && !session.EscalationAlertSent)
        {
            if (session.IsReminderDue())
            {
                ScheduleNextReminder(session);

                return new ChargingAlertDecision(
                    ChargingAlertType.Reminder,
                    $"Battery remains connected at {percentage}%. Your charging target was {target}%.",
                    percentage,
                    target,
                    playSound: _settings.SoundEnabled);
            }
        }

        return null;
    }

    private void ScheduleFirstReminder(ChargingSession session)
    {
        if (_settings.RepeatedRemindersEnabled)
        {
            session.NextReminderDue = _clock.UtcNow + _settings.FirstReminderDelay;
        }
    }

    private void ScheduleNextReminder(ChargingSession session)
    {
        session.NextReminderDue = _clock.UtcNow + _settings.RepeatedReminderInterval;
    }

    /// <summary>
    /// Enables temporary 100% charging mode for the current session.
    /// </summary>
    public void EnableTemporaryFullChargeMode()
    {
        if (_currentSession != null)
        {
            _currentSession.EnableTemporaryFullChargeMode();
        }
    }

    /// <summary>
    /// Disables temporary 100% mode and restores normal target.
    /// </summary>
    public void DisableTemporaryFullChargeMode()
    {
        if (_currentSession != null)
        {
            _currentSession.DisableTemporaryFullChargeMode(_settings.NormalTargetPercentage);
        }
    }

    /// <summary>
    /// Snoozes reminders for the specified duration.
    /// </summary>
    public void Snooze(TimeSpan duration)
    {
        if (_currentSession != null)
        {
            _currentSession.SnoozeExpiration = _clock.UtcNow + duration;
        }
    }

    /// <summary>
    /// Pauses all alerts.
    /// </summary>
    public void PauseAlerts()
    {
        if (_currentSession != null)
        {
            _currentSession.AreAlertsPaused = true;
        }
    }

    /// <summary>
    /// Resumes alerts.
    /// </summary>
    public void ResumeAlerts()
    {
        if (_currentSession != null)
        {
            _currentSession.AreAlertsPaused = false;
        }
    }

    /// <summary>
    /// Updates the settings used by the evaluator.
    /// </summary>
    public void UpdateSettings(ChargeGuardSettings newSettings)
    {
        _settings.NormalTargetPercentage = newSettings.NormalTargetPercentage;
        _settings.AdvanceWarningEnabled = newSettings.AdvanceWarningEnabled;
        _settings.AdvanceWarningPercentage = newSettings.AdvanceWarningPercentage;
        _settings.RepeatedRemindersEnabled = newSettings.RepeatedRemindersEnabled;
        _settings.FirstReminderDelay = newSettings.FirstReminderDelay;
        _settings.RepeatedReminderInterval = newSettings.RepeatedReminderInterval;
        _settings.EscalationPercentage = newSettings.EscalationPercentage;
        _settings.SoundEnabled = newSettings.SoundEnabled;

        // Update session target if not in temporary mode
        if (_currentSession != null && !_currentSession.IsTemporaryFullChargeMode)
        {
            _currentSession.ActiveTargetPercentage = _settings.NormalTargetPercentage;
        }
    }
}
