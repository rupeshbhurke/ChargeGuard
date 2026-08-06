using Microsoft.Data.Sqlite;
using System.IO;

namespace ChargeGuard.Analytics;

/// <summary>
/// Manages the SQLite database for battery analytics.
/// </summary>
public class BatteryDatabase : IDisposable
{
    private readonly string _databasePath;
    private SqliteConnection? _connection;
    private bool _disposed;

    public BatteryDatabase(string databasePath)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
    }

    /// <summary>
    /// Gets the database file path.
    /// </summary>
    public string DatabasePath => _databasePath;

    /// <summary>
    /// Initializes the database and creates schema if needed.
    /// </summary>
    public void Initialize()
    {
        // Ensure directory exists
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create connection
        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        // Create schema
        CreateSchema();
    }

    /// <summary>
    /// Gets an open database connection.
    /// </summary>
    public SqliteConnection GetConnection()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Database not initialized. Call Initialize() first.");
        }

        if (_connection.State != System.Data.ConnectionState.Open)
        {
            _connection.Open();
        }

        return _connection;
    }

    private void CreateSchema()
    {
        if (_connection == null)
            throw new InvalidOperationException("Connection not initialized");

        using var command = _connection.CreateCommand();

        // Create BatteryReadings table
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS BatteryReadings (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp DATETIME NOT NULL,
                battery_percentage INTEGER NOT NULL,
                is_charging BOOLEAN NOT NULL,
                is_ac_connected BOOLEAN NOT NULL,
                is_battery_available BOOLEAN NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_battery_readings_timestamp ON BatteryReadings(timestamp);
            CREATE INDEX IF NOT EXISTS idx_battery_readings_charging ON BatteryReadings(is_charging, timestamp);
        ";
        command.ExecuteNonQuery();

        // Create ChargingSessions table
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ChargingSessions (
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

            CREATE INDEX IF NOT EXISTS idx_charging_sessions_start_time ON ChargingSessions(start_time);
            CREATE INDEX IF NOT EXISTS idx_charging_sessions_end_time ON ChargingSessions(end_time);
        ";
        command.ExecuteNonQuery();

        // Create DailyStats table
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS DailyStats (
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

            CREATE INDEX IF NOT EXISTS idx_daily_stats_date ON DailyStats(date);
        ";
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Close();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}