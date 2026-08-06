using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ChargeGuard.Battery;

/// <summary>
/// P/Invoke declarations for Windows power management APIs.
/// </summary>
public static partial class PowerNativeMethods
{
    /// <summary>
    /// Contains information about the power status of the system.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    /// <summary>
    /// Power setting notification data structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POWERBROADCAST_SETTING
    {
        public Guid PowerSetting;
        public uint DataLength;
        public byte Data;
    }

    /// <summary>
    /// Retrieves the power status of the system.
    /// </summary>
    /// <param name="lpSystemPowerStatus">Pointer to a SYSTEM_POWER_STATUS structure.</param>
    /// <returns>TRUE if the function succeeds, FALSE if it fails.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    /// <summary>
    /// Registers the application to receive power setting notifications.
    /// </summary>
    /// <param name="hRecipient">Handle to the window that will receive notifications.</param>
    /// <param name="PowerSettingGuid">GUID of the power setting.</param>
    /// <returns>Handle to the notification registration, or NULL on failure.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint RegisterPowerSettingNotification(nint hRecipient, Guid PowerSettingGuid);

    /// <summary>
    /// Unregisters a power setting notification.
    /// </summary>
    /// <param name="Handle">Handle to the notification registration.</param>
    /// <returns>TRUE if successful, FALSE if it fails.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterPowerSettingNotification(nint Handle);
}
