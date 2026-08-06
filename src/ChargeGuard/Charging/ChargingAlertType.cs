namespace ChargeGuard.Charging;

/// <summary>
/// Types of charging alerts.
/// </summary>
public enum ChargingAlertType
{
    /// <summary>
    /// Advance warning before reaching the target.
    /// </summary>
    AdvanceWarning,

    /// <summary>
    /// Main target alert when the charging target is reached.
    /// </summary>
    Target,

    /// <summary>
    /// Repeated reminder while charger remains connected.
    /// </summary>
    Reminder,

    /// <summary>
    /// Escalation alert when battery exceeds the target significantly.
    /// </summary>
    Escalation
}
