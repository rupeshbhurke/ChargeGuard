using System.Runtime.InteropServices;
using System.Windows.Forms;
using ChargeGuard.Logging;

namespace ChargeGuard.Battery;

/// <summary>
/// A hidden native window that receives Windows power management notifications.
/// </summary>
public class NativePowerMessageWindow : NativeWindow, IDisposable
{
    private readonly IAppLogger _logger;
    private nint _batteryPercentageNotificationHandle;
    private nint _acdcPowerSourceNotificationHandle;
    private bool _disposed;

    public event EventHandler<BatteryStateChangedEventArgs>? PowerStateChanged;
    public event EventHandler? ResumedFromSleep;

    public NativePowerMessageWindow(IAppLogger logger)
    {
        _logger = logger;

        CreateHandle(new CreateParams
        {
            Parent = nint.Zero,
            ClassName = "Message",
            Style = 0
        });

        RegisterPowerNotifications();
    }

    private void RegisterPowerNotifications()
    {
        try
        {
            if (Handle == nint.Zero)
            {
                _logger.LogError("Cannot register power notifications: window handle is zero");
                return;
            }

            _batteryPercentageNotificationHandle = PowerNativeMethods.RegisterPowerSettingNotification(
                Handle,
                PowerConstants.GUID_BATTERY_PERCENTAGE_REMAINING);

            if (_batteryPercentageNotificationHandle == nint.Zero)
            {
                _logger.LogWarning("Failed to register battery percentage notification (fallback timer will be used)");
            }
            else
            {
                _logger.LogInfo("Registered battery percentage notification");
            }

            _acdcPowerSourceNotificationHandle = PowerNativeMethods.RegisterPowerSettingNotification(
                Handle,
                PowerConstants.GUID_ACDC_POWER_SOURCE);

            if (_acdcPowerSourceNotificationHandle == nint.Zero)
            {
                _logger.LogWarning("Failed to register AC/DC power source notification (fallback timer will be used)");
            }
            else
            {
                _logger.LogInfo("Registered AC/DC power source notification");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to register power notifications", ex);
        }
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg == PowerConstants.WM_POWERBROADCAST)
        {
            HandlePowerBroadcast(m);
        }
    }

    private void HandlePowerBroadcast(Message m)
    {
        switch ((uint)m.WParam)
        {
            case PowerConstants.PBT_POWERSETTINGCHANGE:
                HandlePowerSettingChange(m.LParam);
                break;

            case PowerConstants.PBT_APMRESUMESUSPEND:
            case PowerConstants.PBT_APMRESUMEAUTOMATIC:
                _logger.LogInfo("System resumed from sleep");
                OnResumedFromSleep();
                break;
        }
    }

    private void HandlePowerSettingChange(nint lParam)
    {
        try
        {
            if (lParam == nint.Zero)
            {
                _logger.LogWarning("Power setting change received with null lParam");
                return;
            }

            var setting = Marshal.PtrToStructure<PowerNativeMethods.POWERBROADCAST_SETTING>(lParam);

            if (setting.PowerSetting == PowerConstants.GUID_BATTERY_PERCENTAGE_REMAINING ||
                setting.PowerSetting == PowerConstants.GUID_ACDC_POWER_SOURCE)
            {
                // Query the full power status rather than relying on event payload
                var snapshot = GetCurrentBatterySnapshot();
                OnPowerStateChanged(snapshot);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to handle power setting change", ex);
        }
    }

    private BatterySnapshot GetCurrentBatterySnapshot()
    {
        try
        {
            if (PowerNativeMethods.GetSystemPowerStatus(out var status))
            {
                return BatterySnapshot.FromNativeStatus(status);
            }
            else
            {
                _logger.LogWarning("GetSystemPowerStatus failed");
                return BatterySnapshot.CreateUnavailable();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get system power status", ex);
            return BatterySnapshot.CreateUnavailable();
        }
    }

    private void OnPowerStateChanged(BatterySnapshot snapshot)
    {
        PowerStateChanged?.Invoke(this, new BatteryStateChangedEventArgs(snapshot));
    }

    private void OnResumedFromSleep()
    {
        ResumedFromSleep?.Invoke(this, EventArgs.Empty);
    }

    private void UnregisterPowerNotifications()
    {
        if (_batteryPercentageNotificationHandle != nint.Zero)
        {
            try
            {
                PowerNativeMethods.UnregisterPowerSettingNotification(_batteryPercentageNotificationHandle);
                _logger.LogInfo("Unregistered battery percentage notification");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to unregister battery percentage notification", ex);
            }
            _batteryPercentageNotificationHandle = nint.Zero;
        }

        if (_acdcPowerSourceNotificationHandle != nint.Zero)
        {
            try
            {
                PowerNativeMethods.UnregisterPowerSettingNotification(_acdcPowerSourceNotificationHandle);
                _logger.LogInfo("Unregistered AC/DC power source notification");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to unregister AC/DC power source notification", ex);
            }
            _acdcPowerSourceNotificationHandle = nint.Zero;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        UnregisterPowerNotifications();

        try
        {
            if (Handle != nint.Zero)
            {
                DestroyHandle();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to destroy message window handle", ex);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
