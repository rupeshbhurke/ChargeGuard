using System.ComponentModel;
using System.Windows.Forms;
using ChargeGuard.Logging;
using Timer = System.Windows.Forms.Timer;

namespace ChargeGuard.Battery;

/// <summary>
/// Monitors battery status using Win32 power notifications with a fallback timer.
/// </summary>
public class BatteryMonitor : IDisposable
{
    private readonly IAppLogger _logger;
    private readonly NativePowerMessageWindow _messageWindow;
    private readonly Timer _fallbackTimer;
    private BatterySnapshot? _lastSnapshot;
    private bool _disposed;

    private const int FallbackIntervalMs = 60000; // 60 seconds

    public event EventHandler<BatteryStateChangedEventArgs>? BatteryStateChanged;

    public BatteryMonitor(IAppLogger logger)
    {
        _logger = logger;

        _messageWindow = new NativePowerMessageWindow(logger);
        _messageWindow.PowerStateChanged += (sender, e) => OnPowerStateChanged(e.CurrentState);
        _messageWindow.ResumedFromSleep += OnResumedFromSleep;

        _fallbackTimer = new Timer { Interval = FallbackIntervalMs };
        _fallbackTimer.Tick += (sender, e) => OnPowerStateChanged(GetCurrentSnapshot());

        _logger.LogInfo("BatteryMonitor initialized");
    }

    /// <summary>
    /// Starts monitoring battery status.
    /// </summary>
    public void Start()
    {
        _fallbackTimer.Start();
        _logger.LogInfo("BatteryMonitor started");

        // Get initial state
        var initialSnapshot = GetCurrentSnapshot();
        OnPowerStateChanged(initialSnapshot);
    }

    /// <summary>
    /// Stops monitoring battery status.
    /// </summary>
    public void Stop()
    {
        _fallbackTimer.Stop();
        _logger.LogInfo("BatteryMonitor stopped");
    }

    private void OnPowerStateChanged(BatterySnapshot currentSnapshot)
    {
        var previousSnapshot = _lastSnapshot;

        // Only raise event if state actually changed
        if (HasStateChanged(previousSnapshot, currentSnapshot))
        {
            _logger.LogDebug($"Battery state changed: {previousSnapshot} -> {currentSnapshot}");
            _lastSnapshot = currentSnapshot;
            BatteryStateChanged?.Invoke(this, new BatteryStateChangedEventArgs(currentSnapshot, previousSnapshot));
        }
    }

    private void OnResumedFromSleep(object? sender, EventArgs e)
    {
        _logger.LogInfo("Resumed from sleep, waiting for battery info to stabilize");

        // Wait briefly for battery info to stabilize after resume
        Task.Delay(2000).ContinueWith(_ =>
        {
            var snapshot = GetCurrentSnapshot();
            _logger.LogInfo($"Battery state after resume: {snapshot}");

            // Treat as a state change to trigger re-evaluation
            _lastSnapshot = null; // Force state change detection
            OnPowerStateChanged(snapshot);
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private BatterySnapshot GetCurrentSnapshot()
    {
        try
        {
            if (PowerNativeMethods.GetSystemPowerStatus(out var status))
            {
                return BatterySnapshot.FromNativeStatus(status);
            }
            else
            {
                _logger.LogWarning("GetSystemPowerStatus failed in fallback timer");
                return BatterySnapshot.CreateUnavailable();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get system power status in fallback timer", ex);
            return BatterySnapshot.CreateUnavailable();
        }
    }

    private bool HasStateChanged(BatterySnapshot? previous, BatterySnapshot current)
    {
        if (previous == null)
            return true;

        if (previous.IsAcPowerConnected != current.IsAcPowerConnected)
            return true;

        if (previous.IsCharging != current.IsCharging)
            return true;

        if (previous.IsBatteryAvailable != current.IsBatteryAvailable)
            return true;

        if (previous.BatteryPercentage != current.BatteryPercentage)
            return true;

        return false;
    }

    /// <summary>
    /// Gets the current battery snapshot synchronously.
    /// </summary>
    public BatterySnapshot GetCurrentBatterySnapshot()
    {
        return GetCurrentSnapshot();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _fallbackTimer.Stop();
            _fallbackTimer.Dispose();

            _messageWindow.ResumedFromSleep -= OnResumedFromSleep;
            _messageWindow.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
