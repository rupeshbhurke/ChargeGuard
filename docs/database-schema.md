# Battery Analytics Database Schema

## Overview
SQLite database for tracking battery performance, charging patterns, and health metrics over time.

## Tables

### BatteryReadings
Stores individual battery snapshots collected at regular intervals.

```sql
CREATE TABLE BatteryReadings (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp DATETIME NOT NULL,
    battery_percentage INTEGER NOT NULL,
    is_charging BOOLEAN NOT NULL,
    is_ac_connected BOOLEAN NOT NULL,
    is_battery_available BOOLEAN NOT NULL
);

CREATE INDEX idx_battery_readings_timestamp ON BatteryReadings(timestamp);
CREATE INDEX idx_battery_readings_charging ON BatteryReadings(is_charging, timestamp);
```

### ChargingSessions
Stores complete charging sessions with start/end times and statistics.

```sql
CREATE TABLE ChargingSessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    start_time DATETIME NOT NULL,
    end_time DATETIME,
    start_percentage INTEGER NOT NULL,
    end_percentage INTEGER,
    duration_minutes REAL,
    was_overcharged BOOLEAN DEFAULT 0,
    overcharge_duration_minutes REAL DEFAULT 0,
    target_percentage INTEGER,
    notes TEXT
);

CREATE INDEX idx_charging_sessions_start_time ON ChargingSessions(start_time);
CREATE INDEX idx_charging_sessions_end_time ON ChargingSessions(end_time);
```

### DailyStats
Aggregated daily statistics for quick reporting.

```sql
CREATE TABLE DailyStats (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    date DATE NOT NULL UNIQUE,
    total_charging_time_minutes REAL DEFAULT 0,
    total_discharging_time_minutes REAL DEFAULT 0,
    number_of_charging_sessions INTEGER DEFAULT 0,
    average_charge_time_minutes REAL,
    average_discharge_time_minutes REAL,
    max_battery_percentage INTEGER,
    min_battery_percentage INTEGER,
    overcharge_minutes REAL DEFAULT 0,
    number_of_overcharge_events INTEGER DEFAULT 0
);

CREATE INDEX idx_daily_stats_date ON DailyStats(date);
```

## Key Features

### Data Collection
- **Battery percentage tracking**: Record battery level at regular intervals (e.g., every 5 minutes)
- **Charging status**: Track when battery is charging vs discharging
- **Session detection**: Automatically detect charging session start/end
- **Overcharge detection**: Identify when battery exceeds target percentage

### Analytics Capabilities
- **Charge time analysis**: How long to charge from X% to Y%
- **Discharge time analysis**: How long to discharge from X% to Y%
- **Overcharge tracking**: Duration and frequency of overcharge events
- **Health trends**: Battery capacity degradation over time
- **Pattern analysis**: Daily/weekly charging patterns

### Reporting
- **Daily reports**: Summary of battery activity for a specific day
- **Duration reports**: Custom date range analysis
- **Session history**: List of all charging sessions with details
- **Statistics dashboard**: Key metrics and trends

## Sample Queries

### Get charging sessions for a date range
```sql
SELECT * FROM ChargingSessions
WHERE date(start_time) >= '2026-08-01'
  AND date(start_time) <= '2026-08-06'
ORDER BY start_time DESC;
```

### Calculate average charge time
```sql
SELECT AVG(duration_minutes) as avg_charge_time
FROM ChargingSessions
WHERE duration_minutes IS NOT NULL;
```

### Find overcharge events
```sql
SELECT * FROM ChargingSessions
WHERE was_overcharged = 1
ORDER BY start_time DESC;
```

### Get battery percentage trend for a day
```sql
SELECT timestamp, battery_percentage, is_charging
FROM BatteryReadings
WHERE date(timestamp) = '2026-08-06'
ORDER BY timestamp;
```

### Calculate daily statistics
```sql
SELECT date(timestamp) as date,
       AVG(CASE WHEN is_charging = 1 THEN battery_percentage END) as avg_charge_level,
       AVG(CASE WHEN is_charging = 0 THEN battery_percentage END) as avg_discharge_level,
       MIN(battery_percentage) as min_level,
       MAX(battery_percentage) as max_level
FROM BatteryReadings
GROUP BY date(timestamp);
```
