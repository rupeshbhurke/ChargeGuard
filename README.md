# ChargeGuard

A lightweight Windows 11 desktop utility that monitors your laptop's battery and alerts you when it reaches a configured charging percentage.

## Features

- **Battery Monitoring**: Real-time monitoring of battery percentage and charging status using Windows native power APIs
- **Configurable Alerts**: Set your preferred charging target (default: 80%) with advance warnings
- **Repeated Reminders**: Optional reminders if you forget to disconnect the charger
- **Escalation Alerts**: Stronger alerts when battery exceeds target significantly
- **Temporary 100% Mode**: Quick option to charge to 100% for this session only
- **Tray Icon Application**: Runs unobtrusively in the notification area
- **Privacy-Focused**: No network calls, telemetry, or cloud services
- **Lightweight**: Minimal resource usage with no heavy dependencies

## Technology Stack

- **Language**: C# (.NET 9.0)
- **Framework**: Windows Forms
- **Native APIs**: Direct Win32 power-event APIs through P/Invoke
- **Deployment**: Framework-dependent Windows deployment
- **Target**: Windows 11

## Prerequisites

- Windows 11
- .NET 9.0 Desktop Runtime (or .NET 9.0 SDK for building from source)

## Build Instructions

```powershell
# Restore dependencies
dotnet restore

# Build the solution
dotnet build -c Release

# Run tests
dotnet test -c Release

# Publish for distribution
dotnet publish src/ChargeGuard/ChargeGuard.csproj -c Release -r win-x64 --self-contained false
```

The published application will be in `src\ChargeGuard\bin\Release\net9.0-windows\win-x64\publish\`

## Test Instructions

```powershell
# Run all unit tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Installation

### Using the Installer (Recommended)

1. Download and run the Inno Setup installer (if available)
2. Follow the installation wizard
3. Optionally enable "Start with Windows" during installation

### Manual Installation

1. Download the published application files
2. Extract to a folder of your choice (e.g., `C:\Program Files\ChargeGuard`)
3. Create a desktop shortcut if desired
4. Run `ChargeGuard.exe` to start the application

## Settings File Location

Settings are stored in:
```
%LocalAppData%\ChargeGuard\settings.json
```

## Log File Location

Logs are stored in:
```
%LocalAppData%\ChargeGuard\Logs\
```

Log files are automatically rotated with a maximum of 10 files at 5 MB each.

## Startup Behavior

- By default, ChargeGuard starts with Windows and runs minimized in the notification area
- Startup registration is per-user (no administrator privileges required)
- You can change startup behavior in the settings window

## Privacy Statement

ChargeGuard is designed with privacy in mind:

- **No Network Access**: ChargeGuard does not make any network calls
- **No Telemetry**: No usage data or analytics are collected
- **No Cloud Services**: All data stays on your local machine
- **Local Storage Only**: Settings and logs are stored in your user profile directory
- **Open Source**: The source code is available for audit

## Known Limitations

- **Monitoring Only**: ChargeGuard only monitors battery status. It does not control or stop charging. You must manually disconnect the charger.
- **Firmware Variations**: Different laptop firmware may report battery events differently. A 60-second fallback timer is used for reliability.
- **Windows 11 Only**: Designed and tested for Windows 11. May work on Windows 10 but not officially supported.
- **Single Battery**: Assumes a single primary battery. Multi-battery systems are not supported.

## Usage

### Basic Usage

1. Run ChargeGuard - it will appear in the notification area
2. Connect your laptop charger
3. ChargeGuard will detect the charging session
4. When your battery reaches the target (default: 80%), you'll receive an alert
5. Disconnect the charger when alerted

### Tray Icon Menu

Right-click the tray icon to access:

- **Open ChargeGuard**: Open the settings window
- **Charge to 100% this session**: Temporarily set target to 100% for this charging session
- **Snooze reminders for 10 minutes**: Suppress reminders for 10 minutes
- **Pause alerts**: Temporarily pause all alerts
- **Resume alerts**: Resume alert notifications
- **Start with Windows**: Toggle automatic startup
- **View latest log**: Open the most recent log file
- **About**: View application information
- **Exit**: Close the application

### Settings

Configure the following in the settings window:

- **Normal Target Percentage**: Your preferred charging target (1-100%)
- **Enable Advance Warning**: Show a warning before reaching the target
- **Advance Warning Percentage**: When to show the advance warning
- **Enable Repeated Reminders**: Show reminders if charger remains connected
- **First Reminder Delay**: Time before the first reminder
- **Repeated Reminder Interval**: Time between subsequent reminders
- **Escalation Percentage**: When to show a stronger escalation alert
- **Enable Sound**: Play notification sounds
- **Start with Windows**: Automatically start with Windows
- **Start Minimized**: Start minimized to the notification area

## Architecture

For detailed architecture information, see [docs/architecture.md](docs/architecture.md).

## License

[Specify your license here]

## Contributing

[Specify contribution guidelines here]

## Support

For issues and questions, please use the project's issue tracker.
