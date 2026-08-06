namespace ChargeGuard.Analytics;

/// <summary>
/// Represents a complete charging session.
/// </summary>
public class ChargingSession
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int StartPercentage { get; set; }
    public int? EndPercentage { get; set; }
    public double? DurationMinutes { get; set; }
    public bool WasOvercharged { get; set; }
    public double OverchargeDurationMinutes { get; set; }
    public int? TargetPercentage { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Gets whether this session is currently active (not ended).
    /// </summary>
    public bool IsActive => EndTime == null;

    /// <summary>
    /// Gets the percentage gained during this session.
    /// </summary>
    public int? PercentageGained => EndPercentage.HasValue ? EndPercentage.Value - StartPercentage : null;
}