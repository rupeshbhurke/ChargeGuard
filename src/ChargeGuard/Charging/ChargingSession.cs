namespace ChargeGuard.Charging;

/// <summary>
/// Manages the state of a single charging session.
/// </summary>
public class ChargingSession
{
    private readonly Guid _sessionId;
    private readonly DateTime _startTime;
    private readonly IClock _clock;

    /// <summary>
    /// Gets the unique session identifier.
    /// </summary>
    public Guid SessionId => _sessionId;

    /// <summary>
    /// Gets when the session started.
    /// </summary>
    public DateTime StartTime => _startTime;

    /// <summary>
    /// Gets or sets the active target percentage for this session.
    /// </summary>
    public int ActiveTargetPercentage { get; set; }

    /// <summary>
    /// Gets or sets whether temporary 100% mode is active.
    /// </summary>
    public bool IsTemporaryFullChargeMode { get; set; }

    /// <summary>
    /// Gets or sets whether the advance warning has been sent.
    /// </summary>
    public bool AdvanceWarningSent { get; set; }

    /// <summary>
    /// Gets or sets whether the target alert has been sent.
    /// </summary>
    public bool TargetAlertSent { get; set; }

    /// <summary>
    /// Gets or sets whether the escalation alert has been sent.
    /// </summary>
    public bool EscalationAlertSent { get; set; }

    /// <summary>
    /// Gets or sets when the next reminder is due.
    /// </summary>
    public DateTime? NextReminderDue { get; set; }

    /// <summary>
    /// Gets or sets when snooze expires.
    /// </summary>
    public DateTime? SnoozeExpiration { get; set; }

    /// <summary>
    /// Gets or sets whether alerts are paused.
    /// </summary>
    public bool AreAlertsPaused { get; set; }

    /// <summary>
    /// Gets or sets the last battery percentage seen in this session.
    /// </summary>
    public int? LastBatteryPercentage { get; set; }

    public ChargingSession(int targetPercentage, IClock clock)
    {
        _sessionId = Guid.NewGuid();
        _startTime = clock.UtcNow;
        _clock = clock;
        ActiveTargetPercentage = targetPercentage;
    }

    /// <summary>
    /// Checks whether the session is currently snoozed.
    /// </summary>
    public bool IsSnoozed()
    {
        if (!SnoozeExpiration.HasValue)
            return false;

        return _clock.UtcNow < SnoozeExpiration.Value;
    }

    /// <summary>
    /// Checks whether a reminder is due.
    /// </summary>
    public bool IsReminderDue()
    {
        if (!NextReminderDue.HasValue)
            return false;

        return _clock.UtcNow >= NextReminderDue.Value;
    }

    /// <summary>
    /// Clears all alert states (used when session ends).
    /// </summary>
    public void ClearAlertStates()
    {
        AdvanceWarningSent = false;
        TargetAlertSent = false;
        EscalationAlertSent = false;
        NextReminderDue = null;
        SnoozeExpiration = null;
        LastBatteryPercentage = null;
    }

    /// <summary>
    /// Enables temporary 100% mode.
    /// </summary>
    public void EnableTemporaryFullChargeMode()
    {
        IsTemporaryFullChargeMode = true;
        ActiveTargetPercentage = 100;
        // Clear all alert states for the new target
        AdvanceWarningSent = false;
        TargetAlertSent = false;
        EscalationAlertSent = false;
        NextReminderDue = null;
    }

    /// <summary>
    /// Disables temporary 100% mode and restores normal target.
    /// </summary>
    public void DisableTemporaryFullChargeMode(int normalTarget)
    {
        IsTemporaryFullChargeMode = false;
        ActiveTargetPercentage = normalTarget;
        ClearAlertStates();
    }
}
