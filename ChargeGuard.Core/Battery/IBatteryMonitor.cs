namespace ChargeGuard.Core.Battery;

/// <summary>
/// Interface for platform-specific battery monitoring implementations.
/// </summary>
public interface IBatteryMonitor : IDisposable
{
    /// <summary>
    /// Event raised when the battery state changes.
    /// </summary>
    event EventHandler<BatteryStateChangedEventArgs>? BatteryStateChanged;

    /// <summary>
    /// Starts monitoring battery status.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops monitoring battery status.
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets the current battery snapshot synchronously.
    /// </summary>
    BatterySnapshot GetCurrentBatterySnapshot();
}

/// <summary>
/// Event arguments for battery state changes.
/// </summary>
public class BatteryStateChangedEventArgs : EventArgs
{
    public BatterySnapshot CurrentState { get; }
    public BatterySnapshot? PreviousState { get; }

    public BatteryStateChangedEventArgs(BatterySnapshot currentState, BatterySnapshot? previousState = null)
    {
        CurrentState = currentState;
        PreviousState = previousState;
    }
}