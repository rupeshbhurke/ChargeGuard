namespace ChargeGuard.Logging;

/// <summary>
/// Application logger interface for writing log messages.
/// </summary>
public interface IAppLogger
{
    /// <summary>
    /// Writes an informational log message.
    /// </summary>
    void LogInfo(string message);

    /// <summary>
    /// Writes a warning log message.
    /// </summary>
    void LogWarning(string message);

    /// <summary>
    /// Writes an error log message.
    /// </summary>
    void LogError(string message, Exception? exception = null);

    /// <summary>
    /// Writes a debug log message.
    /// </summary>
    void LogDebug(string message);
}
