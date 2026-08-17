# ChargeGuard

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Platform](https://img.shields.io/badge/Platform-Windows%2011-blue.svg)](https://www.microsoft.com/windows)

A lightweight Windows 11 desktop utility that monitors your laptop's battery and alerts you when it reaches a configured charging percentage.

## 🚀 Quick Start

1. **Download the latest release** from the [Releases page](https://github.com/rupeshbhurke/ChargeGuard/releases)
2. **Install .NET 9.0 Desktop Runtime** if not already installed
3. **Run ChargeGuard.exe** - it will appear in your notification area
4. **Connect your charger** and let ChargeGuard monitor your battery

## 📖 Features

- **Battery Monitoring**: Real-time monitoring of battery percentage and charging status using Windows native power APIs
- **Configurable Alerts**: Set your preferred charging target (default: 80%) with advance warnings
- **Repeated Reminders**: Optional reminders if you forget to disconnect the charger
- **Escalation Alerts**: Stronger alerts when battery exceeds target significantly
- **Temporary 100% Mode**: Quick option to charge to 100% for this session only
- **Configurable Notification Timeout**: Adjust how long alerts display before auto-dismissing (3-60 seconds, default 10)
- **Battery Analytics**: Track charging patterns, discharge rates, and battery health over time
- **Interactive Dashboard**: WebView2-based web dashboard with interactive charts using Plotly.js
- **Daily Statistics**: View average charging/discharging times, session counts, and overcharge tracking
- **Tray Icon Application**: Runs unobtrusively in the notification area
- **Privacy-Focused**: No network calls, telemetry, or cloud services
- **Lightweight**: Minimal resource usage with no heavy dependencies

## Technology Stack

- **Language**: C# (.NET 9.0)
- **Framework**: Windows Forms (Windows), AvaloniaUI (Linux - in development)
- **Native APIs**: Direct Win32 power-event APIs through P/Invoke (Windows), UPower D-Bus (Linux - planned)
- **Database**: SQLite for battery analytics storage
- **Web Dashboard**: WebView2 with Plotly.js for interactive charts
- **Deployment**: Framework-dependent deployment
- **Target**: Windows 11 (current), Ubuntu Linux (in development)

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

1. Download the latest installer from the [Releases page](https://github.com/rupeshbhurke/ChargeGuard/releases)
2. Run the `ChargeGuard-Setup.exe` installer (requires administrator privileges)
3. Follow the installation wizard
4. The installer will check for .NET 9.0 Desktop Runtime and warn if not installed
5. Optionally enable "Start with Windows" during installation

### Manual Installation

1. Download the latest release from the [Releases page](https://github.com/rupeshbhurke/ChargeGuard/releases)
2. Extract the files to a folder of your choice (e.g., `C:\Program Files\ChargeGuard`)
3. Create a desktop shortcut if desired
4. Run `ChargeGuard.exe` to start the application

### Building from Source

See the [Build Instructions](#build-instructions) section below.

## Settings File Location

Settings are stored in:
```
%LocalAppData%\ChargeGuard\settings.json
```

## Database File Location

Battery analytics database is stored in:
```
%LocalAppData%\ChargeGuard\battery_analytics.db
```

## Log File Location

Logs are stored in:
```
%LocalAppData%\ChargeGuard\Logs\
```

Log files are automatically rotated with a maximum of 10 files at 5 MB each.

## Startup Behavior

- By default, ChargeGuard starts with Windows and runs minimized in the notification area
- Startup registration is per-user (HKCU registry)
- You can change startup behavior in the settings window
- Note: The installer requires administrator privileges to check for .NET 9.0 Desktop Runtime

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
- **Notification Timeout**: How long alerts display before auto-dismissing (3-60 seconds, default 10)

### Analytics Dashboard

Access the battery analytics dashboard from the tray icon menu:

- **Battery Percentage Over Time**: Visual chart showing battery level changes
- **Charging Pattern**: Timeline of charging/discharging periods
- **Charging Sessions**: Detailed list of all charging sessions
- **Battery Readings**: Raw battery data with timestamps
- **Web Dashboard**: Interactive charts with Plotly.js for advanced analysis

The dashboard tracks:
- Average charging and discharging times
- Total charging sessions and overcharge events
- Daily battery statistics
- Battery percentage trends over time

## Architecture

ChargeGuard uses a hybrid architecture with shared core logic and platform-specific implementations:

### Project Structure

- **ChargeGuard.Core**: Shared business logic (battery monitoring interfaces, settings management, alert evaluation)
- **ChargeGuard**: Windows-specific implementation using Windows Forms and Win32 APIs
- **ChargeGuard.Linux**: Linux-specific implementation using AvaloniaUI and UPower (in development)

### Cross-Platform Design

The application is designed to support multiple platforms while maintaining a single source of truth for core functionality:

- **Shared Core**: Battery monitoring interfaces, settings management, alert evaluation logic
- **Platform-Specific UI**: Windows Forms for Windows, AvaloniaUI for Linux
- **Platform-Specific Services**: Win32 APIs for Windows, UPower D-Bus for Linux

For detailed architecture information, see [docs/architecture.md](docs/architecture.md).

### Recent UI Improvements

The settings window has been modernized with:
- Professional blue header with app branding
- Modern flat design with improved color scheme
- Better typography and spacing
- Right-aligned numeric inputs for consistency
- Enhanced button styling with icons
- Improved layout with two-column design

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## Support

For issues, questions, or suggestions, please:
- Open an issue on [GitHub Issues](https://github.com/rupeshbhurke/ChargeGuard/issues)
- Check existing issues for solutions
- Review the [architecture documentation](docs/architecture.md) for technical details

## Acknowledgments

- Built with [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- Uses [Windows Forms](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/)
- Powered by Windows native power management APIs

## Repository

- **GitHub**: https://github.com/rupeshbhurke/ChargeGuard
- **Issues**: https://github.com/rupeshbhurke/ChargeGuard/issues
- **Releases**: https://github.com/rupeshbhurke/ChargeGuard/releases
