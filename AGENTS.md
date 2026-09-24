# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Build, test, run

```powershell
dotnet restore
dotnet build -c Release

# run all tests
dotnet test

# run single test
dotnet test --filter "FullyQualifiedName~ChargingAlertEvaluatorTests.MethodName"

# publish Windows build
dotnet publish src/ChargeGuard/ChargeGuard.csproj -c Release -r win-x64 --self-contained false

# Linux (in development)
dotnet build ChargeGuard.Linux/ChargeGuard.Linux.csproj -c Release
dotnet run --project ChargeGuard.Linux/ChargeGuard.Linux.csproj
```

Solution: `ChargeGuard.sln`. Projects: `ChargeGuard.Core` (shared logic), `src/ChargeGuard` (Windows Forms app), `ChargeGuard.Linux` (AvaloniaUI, in development), `tests/ChargeGuard.Tests` (xUnit-style tests).

Target framework: .NET 9.0. Windows app uses Windows Forms + Win32 P/Invoke; no unit tests exist for UI — only `ChargingAlertEvaluator` (core business logic) is unit tested, using `TestClock` for deterministic time.

## Architecture

Full details in `docs/architecture.md` — read it for anything touching battery monitoring, alert logic, or the Win32 notification flow. Key shape:

**Layered, event-driven, single-threaded (UI thread).** No shared mutable state across threads except file logging (lock-guarded).

- **Battery Monitoring (`Battery/`)** — `NativePowerMessageWindow` is a hidden window receiving `WM_POWERBROADCAST`. `BatteryMonitor` wraps it, adds a 60-second fallback timer, and raises `BatteryStateChanged` with an immutable `BatterySnapshot`.
- **Charging Session (`Charging/`)** — `ChargingAlertEvaluator` is the core state machine: `EvaluateState(BatterySnapshot)` → `ChargingAlertDecision?`. Alert types: AdvanceWarning, Target, Reminder, Escalation. Uses hysteresis (crossing detection vs. previous percentage) so each alert fires once per session. `ChargingSession` holds per-session state (target, temp-100% mode, sent-alert flags, snooze/pause). This is the piece with real unit test coverage — any change to alert semantics needs matching tests in `tests/ChargeGuard.Tests/ChargingAlertEvaluatorTests.cs`.
- **Notifications (`Notifications/`)** — `IAlertNotifier`/`NotifyIconAlertNotifier` (WinForms balloon tips), `ISoundPlayer`/`SystemSoundPlayer`.
- **Settings (`Settings/`)** — `ChargeGuardSettings` + `SettingsManager` (atomic JSON writes, corrupt-file backup+recovery) at `%LocalAppData%\ChargeGuard\settings.json`. `StartupManager` handles per-user HKCU run-key registration (no elevation).
- **Logging (`Logging/`)** — `RollingFileLogger`, rotating at 5 MB / 10 files, under `%LocalAppData%\ChargeGuard\Logs\`.
- **Application (`Application/`)** — `ChargeGuardApplicationContext` is the composition root: owns the tray `NotifyIcon`, wires monitor → evaluator → notifier, runs the 1-second reminder-check timer. `SingleInstanceManager` enforces one instance via a named mutex.
- **UI (`UI/`)** — `SettingsForm`, `AboutForm`, `AnalyticsWindow` (Windows Forms dialogs).
- **Analytics (`Analytics/`)** — SQLite-backed (`BatteryDatabase`) session/reading history; `BatteryAnalyticsService` collects data from `BatteryMonitor`; `BatteryAnalyticsQueries` serves the dashboard. Schema in `docs/database-schema.md`.
- **Dashboard (`Dashboard/`)** — static HTML/CSS/JS rendered in a WebView2 control, charts via Plotly.js, driven by a C# message bridge.

### Event flow (Windows)
`WM_POWERBROADCAST` → `NativePowerMessageWindow` → `BatteryMonitor` (dedupes state) → `ChargeGuardApplicationContext` → `ChargingAlertEvaluator.EvaluateState` → alert decision → `NotifyIconAlertNotifier` + `SystemSoundPlayer`.

### Deliberate design constraints (don't undo without reason)
- No network calls, telemetry, or cloud services anywhere in the app — privacy is a stated product requirement.
- Framework-dependent deployment only (not self-contained) — keep publish size small.
- Settings persistence is plain JSON, not a database — analytics data is the only thing that goes in SQLite.
- Single primary battery assumed; multi-battery systems are out of scope.
- Monitoring only — the app never controls/stops charging, only alerts.
