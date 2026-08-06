namespace ChargeGuard.Core.Charging;

/// <summary>
/// Interface for clock abstraction to enable testing.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

/// <summary>
/// Default implementation using system clock.
/// </summary>
public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}