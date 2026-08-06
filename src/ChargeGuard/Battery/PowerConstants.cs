using System.Runtime.InteropServices;

namespace ChargeGuard.Battery;

/// <summary>
/// Constants and GUIDs for Windows power management APIs.
/// </summary>
public static class PowerConstants
{
    // Window messages
    public const int WM_POWERBROADCAST = 0x0218;
    public const int PBT_POWERSETTINGCHANGE = 0x8013;
    public const int PBT_APMRESUMESUSPEND = 0x0007;
    public const int PBT_APMRESUMEAUTOMATIC = 0x0012;

    // Power setting GUIDs
    public static readonly Guid GUID_BATTERY_PERCENTAGE_REMAINING = new("A7AD8041-BD45-4CAE-B99A-385129B32944");
    public static readonly Guid GUID_ACDC_POWER_SOURCE = new("5D3E9A59-E9D5-4B99-A672-FAF80F1A8C68");

    // AC line status
    public const byte AC_LINE_OFFLINE = 0;
    public const byte AC_LINE_ONLINE = 1;
    public const byte AC_LINE_UNKNOWN = 255;

    // Battery charge status
    public const byte BATTERY_FLAG_HIGH = 8;
    public const byte BATTERY_FLAG_LOW = 4;
    public const byte BATTERY_FLAG_CRITICAL = 2;
    public const byte BATTERY_FLAG_CHARGING = 8;
    public const byte BATTERY_FLAG_NO_BATTERY = 128;
    public const byte BATTERY_FLAG_UNKNOWN = 255;

    // Battery life percent
    public const byte BATTERY_PERCENTAGE_UNKNOWN = 255;
}
