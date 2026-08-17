# ChargeGuard Architecture

## Overview

ChargeGuard is designed as a lightweight, event-driven Windows application with clear separation of concerns. The architecture prioritizes minimal resource usage, privacy, and reliability.

## Core Components

### 1. Battery Monitoring Layer (`Battery/`)

#### PowerNativeMethods.cs
- P/Invoke declarations for Windows power management APIs
- Uses modern source-generated P/Invoke with `LibraryImport`
- Key APIs:
  - `GetSystemPowerStatus`: Queries current battery and AC status
  - `RegisterPowerSettingNotification`: Registers for power setting changes
  - `UnregisterPowerSettingNotification`: Unregisters notifications
- Defines power setting GUIDs:
  - `GUID_BATTERY_PERCENTAGE_REMAINING`: Battery percentage changes
  - `GUID_ACDC_POWER_SOURCE`: AC/battery power source changes

#### NativePowerMessageWindow.cs
- Hidden native window that receives Windows power notifications
- Inherits from `NativeWindow` for lightweight message handling
- Processes `WM_POWERBROADCAST` messages
- Handles `PBT_POWERSETTINGCHANGE` for battery percentage and power source changes
- Handles `PBT_APMRESUMESUSPEND` and `PBT_APMRESUMEAUTOMATIC` for sleep/resume
- Properly disposes native handles on shutdown

#### BatterySnapshot.cs
- Immutable snapshot of current battery state
- Contains:
  - AC power connection status
  - Battery percentage (1-100, or null if unknown)
  - Charging status
  - Battery availability
- Created from native `SYSTEM_POWER_STATUS` structure
- Handles edge cases (no battery, unknown percentage)

#### BatteryMonitor.cs
- Main battery monitoring service
- Uses `NativePowerMessageWindow` for event-driven monitoring
- Implements 60-second fallback timer for reliability
- Handles sleep/resume scenarios with stabilization delay
- Raises `BatteryStateChanged` events when state changes
- Prevents duplicate events through state comparison

### 2. Charging Session Management Layer (`Charging/`)

#### ChargingSession.cs
- Represents a single charging session (from AC connect to disconnect)
- Tracks:
  - Session ID and start time
  - Active target percentage
  - Temporary 100% mode flag
  - Alert state (advance, target, escalation sent)
  - Reminder scheduling
  - Snooze and pause states
  - Last battery percentage
- Methods:
  - `IsSnoozed()`: Checks if currently snoozed
  - `IsReminderDue()`: Checks if reminder is due
  - `ClearAlertStates()`: Resets all alert states
  - `EnableTemporaryFullChargeMode()`: Enables 100% mode
  - `DisableTemporaryFullChargeMode()`: Restores normal target

#### ChargingAlertEvaluator.cs
- Core business logic for alert decisions
- State machine-based evaluation to prevent duplicate alerts
- Key method: `EvaluateState(BatterySnapshot)` returns `ChargingAlertDecision?`
- Alert types:
  - `AdvanceWarning`: When crossing advance warning threshold
  - `Target`: When crossing target threshold
  - `Reminder`: When reminder timer expires
  - `Escalation`: When crossing escalation threshold
- Uses hysteresis to prevent alert spam from percentage fluctuations
- Supports:
  - Temporary 100% mode
  - Snooze functionality
  - Pause/resume alerts
  - Settings updates

#### IClock.cs
- Clock abstraction for testable time handling
- `SystemClock` implementation uses real system time
- `TestClock` implementation for unit tests with controllable time

### 3. Notification Layer (`Notifications/`)

#### IAlertNotifier.cs
- Interface for alert notification implementations
- `ShowAlert(ChargingAlertDecision)`: Display alert to user
- `UpdateTooltip(string)`: Update tray icon tooltip

#### NotifyIconAlertNotifier.cs
- WinForms `NotifyIcon` implementation
- Shows balloon notifications
- Plays sounds via `ISoundPlayer`
- Handles notification failures gracefully
- Updates tray icon tooltip with current status

#### ISoundPlayer.cs
- Interface for sound playback
- `PlayNotificationSound()`: Play notification sound

#### SystemSoundPlayer.cs
- Uses `SystemSounds.Exclamation.Play()`
- Lightweight, no external audio files required

### 4. Settings Layer (`Settings/`)

#### ChargeGuardSettings.cs
- Contains all application settings
- Properties:
  - Normal target percentage
  - Advance warning settings
  - Repeated reminder settings
  - Escalation percentage
  - Sound enabled
  - Startup with Windows
  - Start minimized
  - Temporary full charge target
  - Notification timeout (3-60 seconds, default 10)
- `ValidateAndNormalize()`: Ensures settings consistency
- `Clone()`: Creates a copy of settings

#### SettingsManager.cs
- Manages settings persistence using JSON
- Stores in `%LocalAppData%\ChargeGuard\settings.json`
- Features:
  - Atomic writes to prevent corruption
  - Default settings on first run
  - Graceful handling of corrupt JSON
  - Backup of corrupt files for diagnostics
  - Versioned settings model

#### StartupManager.cs
- Manages Windows startup registration
- Uses `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- Per-user registration (no elevation required)
- Idempotent enable/disable operations
- Properly quotes executable paths

### 5. Logging Layer (`Logging/`)

#### IAppLogger.cs
- Interface for logging operations
- Methods: `LogInfo`, `LogWarning`, `LogError`, `LogDebug`

#### RollingFileLogger.cs
- Text file logging with automatic rotation
- Stores in `%LocalAppData%\ChargeGuard\Logs\`
- Features:
  - Maximum file size (default: 5 MB)
  - Maximum retained files (default: 10)
  - Automatic cleanup of old files
  - Atomic file operations
  - Graceful handling of write failures
- Log format: `[timestamp] [level] message`

### 6. Application Layer (`Application/`)

#### SingleInstanceManager.cs
- Ensures only one instance runs per user session
- Uses named mutex: `Global\ChargeGuard_SingleInstance_Mutex`
- Returns false if another instance is already running
- Properly releases mutex on disposal

#### ChargeGuardApplicationContext.cs
- Main application context (replaces default Form)
- Manages application lifecycle
- Coordinates all components:
  - Battery monitor
  - Alert evaluator
  - Settings manager
  - Startup manager
  - Alert notifier
  - Sound player
- Creates and manages `NotifyIcon`
- Implements tray icon context menu
- Handles user commands (open settings, snooze, pause, etc.)
- Updates tray tooltip dynamically
- Runs reminder check timer (1-second interval)

### 7. UI Layer (`UI/`)

#### SettingsForm.cs
- Windows Forms settings dialog
- Displays current battery status
- Provides all configurable settings
- Updates in real-time (1-second refresh timer)
- Validates settings before saving
- Updates alert evaluator when settings change

#### AboutForm.cs
- Simple about dialog
- Shows application version
- Displays privacy information

#### AnalyticsWindow.cs
- Battery analytics reporting window
- Multiple tabs: Summary, Sessions, Readings, Web Dashboard
- GDI+ charts for battery percentage and charging patterns
- DataGridView for tabular data display
- WebView2 integration for interactive Plotly.js charts
- Date range filtering for reports

### 8. Analytics Layer (`Analytics/`)

#### BatteryDatabase.cs
- SQLite database management
- Schema creation for BatteryReadings, ChargingSessions, DailyStats tables
- Connection management and disposal
- Stores in `%LocalAppData%\ChargeGuard\battery_analytics.db`

#### BatteryAnalyticsService.cs
- Collects battery readings at regular intervals
- Tracks charging sessions (start/end detection)
- Updates daily statistics (charging/discharging time, session counts)
- Overcharge detection and tracking
- Integrates with BatteryMonitor for data collection

#### BatteryAnalyticsQueries.cs
- Query interface for analytics data
- Methods:
  - `GetStatistics()`: Overall charging statistics
  - `GetChargingSessions()`: Session history for date range
  - `GetBatteryReadings()`: Raw battery data
  - `GetDailyStatistics()`: Daily aggregated stats
  - `GetDischargeReadings()`: Discharge-specific data
  - `IdentifyDischargeSessions()`: Continuous discharge session detection

#### BatteryReading.cs
- DTO for individual battery readings
- Contains timestamp, percentage, charging status, AC connection

#### ChargingSession.cs
- DTO for charging session data
- Contains start/end times, percentages, duration, overcharge info

### 9. Web Dashboard (`Dashboard/`)

#### index.html
- HTML structure for WebView2 dashboard
- Header with stats display
- Chart selector dropdown
- Chart containers for Plotly.js

#### styles.css
- Modern dark theme styling
- Responsive layout
- Chart visibility controls for single-chart mode

#### dashboard.js
- Plotly.js chart rendering
- Chart data visualization (battery trends, charging patterns, discharge analysis)
- Chart selector logic
- C# message bridge for data updates

## Win32 Power Notification Flow

```
1. Application starts
   ↓
2. NativePowerMessageWindow created
   ↓
3. RegisterPowerSettingNotification called for:
   - GUID_BATTERY_PERCENTAGE_REMAINING
   - GUID_ACDC_POWER_SOURCE
   ↓
4. Windows sends WM_POWERBROADCAST message
   ↓
5. NativePowerMessageWindow.WndProc processes message
   ↓
6. If PBT_POWERSETTINGCHANGE:
   - Query GetSystemPowerStatus for full state
   - Raise PowerStateChanged event
   ↓
7. BatteryMonitor receives event
   - Compares with previous state
   - If changed, raises BatteryStateChanged event
   ↓
8. ChargeGuardApplicationContext receives event
   - Passes to ChargingAlertEvaluator
   ↓
9. ChargingAlertEvaluator evaluates state
   - Updates charging session
   - Returns alert decision if needed
   ↓
10. If alert decision:
    - NotifyIconAlertNotifier shows notification
    - SystemSoundPlayer plays sound
```

## Hidden Message Window Lifecycle

```
1. BatteryMonitor created
   ↓
2. NativePowerMessageWindow created
   ↓
3. CreateHandle() called with CreateParams
   ↓
4. RegisterPowerSettingNotification called
   - Returns notification handles
   ↓
5. Window receives messages via WndProc
   ↓
6. On disposal:
   - UnregisterPowerSettingNotification called
   - DestroyHandle() called
   - Notification handles released
```

## Charging Session State Model

### Session Start
- Triggered when power source changes from battery to AC
- New `ChargingSession` created with:
  - Session ID (GUID)
  - Start time (current UTC)
  - Active target (from settings)
  - All alert states cleared

### Session End
- Triggered when power source changes from AC to battery
- Current session disposed
- All timers cleared
- Temporary 100% mode disabled
- Normal target restored

### Alert State Machine

```
Initial State
    ↓
[AC Connected] → Session Started
    ↓
[Cross Advance Warning] → Advance Warning Sent
    ↓
[Cross Target] → Target Alert Sent → Schedule First Reminder
    ↓
[Reminder Due] → Reminder Sent → Schedule Next Reminder
    ↓
[Cross Escalation] → Escalation Sent → Cancel Reminders
    ↓
[AC Disconnected] → Session Ended
```

### Hysteresis and Duplicate Prevention

- Alert flags (`AdvanceWarningSent`, `TargetAlertSent`, `EscalationAlertSent`) prevent re-alerting
- Crossing detection uses previous percentage vs current percentage
- Once an alert is sent, it won't be sent again in the same session
- Session reset (disconnect/reconnect) clears all alert flags
- Temporary 100% mode clears alert flags for the new target

## Sleep/Resume Recovery

```
1. System suspends
   ↓
2. PBT_APMRESUMESUSPEND or PBT_APMRESUMEAUTOMATIC received
   ↓
3. BatteryMonitor waits 2 seconds for battery info to stabilize
   ↓
4. GetSystemPowerStatus queried
   ↓
5. ChargingAlertEvaluator re-evaluates current state
   ↓
6. No duplicate alerts due to session-based state
```

## Resource Footprint Decisions

### Why No Browser Runtime?
- Electron/WebView2 would add 100MB+ overhead
- Windows Forms is native and lightweight
- No network access needed for this application

### Why No Database?
- Settings are simple key-value pairs
- JSON is sufficient and human-readable
- No query complexity needed
- Reduces dependencies and file size

### Why No High-Frequency Polling?
- Event-driven architecture is more efficient
- Win32 power notifications provide real-time updates
- 60-second fallback timer is sufficient for reliability
- Minimizes CPU usage when idle

### Why Framework-Dependent Deployment?
- Reduces application size (~10 MB vs ~100 MB+)
- .NET Desktop Runtime is likely already installed
- Faster startup time
- Smaller download size

### Why No Self-Contained Runtime?
- Increases application size significantly
- .NET Desktop Runtime is a reasonable prerequisite
- Can be installed via Windows Update
- Reduces storage footprint

## Error Handling Strategy

### Battery Information Unavailable
- Log warning and continue
- Return `BatterySnapshot` with unknown percentage
- No alerts generated when percentage is unknown

### No Battery Present
- Log info and end current session
- Return unavailable snapshot
- Application continues running

### Power Notification Registration Fails
- Log error but continue
- Fallback timer provides basic monitoring
- Application remains functional

### Settings File Corrupt
- Backup corrupt file for diagnostics
- Create new default settings
- Log error
- Application continues with defaults

### Notification Failure
- Log error
- Do not crash
- Continue monitoring

### Startup Registration Denied
- Log error
- Continue without startup registration
- User can manually enable later

## Thread Safety

- All components are single-threaded (UI thread)
- No background threads except:
  - Fallback timer (UI thread timer)
  - Reminder check timer (UI thread timer)
  - Sleep/resume delay (Task with synchronization context)
- No shared mutable state between threads
- Lock used only in `RollingFileLogger` for file operations

## Testing Strategy

### Unit Tests
- `ChargingAlertEvaluator` is fully tested with 21 scenarios
- Uses `TestClock` for controllable time
- Tests all alert types and edge cases
- Tests session state transitions
- Tests settings validation

### Integration Tests
- Manual testing on Windows 11
- Verification checklist for all user scenarios
- Real battery charging/discharging cycles
- Sleep/resume testing

### No UI Tests
- UI is simple Windows Forms
- Manual testing is sufficient
- No automated UI testing framework
- Reduces complexity and dependencies
