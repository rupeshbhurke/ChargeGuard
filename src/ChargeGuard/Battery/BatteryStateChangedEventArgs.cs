namespace ChargeGuard.Battery;

/// <summary>
/// Event arguments for battery state changes.
/// </summary>
public class BatteryStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous battery snapshot.
    /// </summary>
    public BatterySnapshot? PreviousState { get; init; }

    /// <summary>
    /// Gets the current battery snapshot.
    /// </summary>
    public BatterySnapshot CurrentState { get; init; }

    /// <summary>
    /// Gets whether this is a resume from sleep event.
    /// </summary>
    public bool IsResumeFromSleep { get; init; }

    public BatteryStateChangedEventArgs(BatterySnapshot currentState, BatterySnapshot? previousState = null, bool isResumeFromSleep = false)
    {
        CurrentState = currentState;
        PreviousState = previousState;
        IsResumeFromSleep = isResumeFromSleep;
    }
}
