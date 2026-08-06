using System.Threading;
using ChargeGuard.Logging;

namespace ChargeGuard.Application;

/// <summary>
/// Ensures only one instance of the application runs per user session.
/// </summary>
public class SingleInstanceManager : IDisposable
{
    private readonly IAppLogger _logger;
    private Mutex? _mutex;
    private bool _disposed;
    private const string MutexName = "Global\\ChargeGuard_SingleInstance_Mutex";

    public SingleInstanceManager(IAppLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to acquire the single-instance mutex.
    /// </summary>
    /// <returns>True if this is the first instance, false if another instance is already running.</returns>
    public bool TryAcquireMutex()
    {
        try
        {
            _mutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                _logger.LogInfo("Another instance is already running");
                return false;
            }

            _logger.LogInfo("Single-instance mutex acquired");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to acquire single-instance mutex", ex);
            // Allow the application to run if mutex creation fails
            return true;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing && _mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
                _logger.LogInfo("Single-instance mutex released");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to release single-instance mutex", ex);
            }
            _mutex = null;
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
