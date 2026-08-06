namespace ChargeGuard.Core.Logging;

/// <summary>
/// Interface for application logging implementations.
/// </summary>
public interface IAppLogger
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    void LogInfo(string message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    void LogWarning(string message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    void LogError(string message);

    /// <summary>
    /// Logs an error message with exception details.
    /// </summary>
    void LogError(string message, Exception exception);

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    void LogDebug(string message);
}