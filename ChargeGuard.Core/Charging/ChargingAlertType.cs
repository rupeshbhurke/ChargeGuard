namespace ChargeGuard.Core.Charging;

/// <summary>
/// Types of charging alerts.
/// </summary>
public enum ChargingAlertType
{
    /// <summary>
    /// Advance warning before reaching target.
    /// </summary>
    AdvanceWarning,

    /// <summary>
    /// Target percentage reached.
    /// </summary>
    Target,

    /// <summary>
    /// Reminder after target reached.
    /// </summary>
    Reminder,

    /// <summary>
    /// Escalation warning when significantly above target.
    /// </summary>
    Escalation
}