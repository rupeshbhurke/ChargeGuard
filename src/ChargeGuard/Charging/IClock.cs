namespace ChargeGuard.Charging;

/// <summary>
/// Clock abstraction for testable time handling.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }
}

/// <summary>
/// Real-time clock implementation using system time.
/// </summary>
public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
