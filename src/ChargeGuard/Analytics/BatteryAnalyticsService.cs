using Microsoft.Data.Sqlite;
using ChargeGuard.Battery;
using ChargeGuard.Logging;
using ChargeGuard.Settings;
using Timer = System.Windows.Forms.Timer;

namespace ChargeGuard.Analytics;

/// <summary>
/// Service for collecting and managing battery analytics data.
/// </summary>
public class BatteryAnalyticsService : IDisposable
{
    private readonly BatteryDatabase _database;
    private readonly IAppLogger _logger;
    private readonly ChargeGuardSettings _settings;
    private readonly Timer _collectionTimer;
    private BatterySnapshot? _lastSnapshot;
    private ChargingSession? _currentChargingSession;
    private DateTime? _overchargeStartTime;
    private bool _disposed;

    public BatteryAnalyticsService(BatteryDatabase database, IAppLogger logger, ChargeGuardSettings settings)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // Collect data every 5 minutes
        _collectionTimer = new Timer { Interval = 5 * 60 * 1000 };
        _collectionTimer.Tick += OnCollectionTimerTick;
    }

    /// <summary>
    /// Starts the analytics data collection.
    /// </summary>
    public void Start()
    {
        _logger.LogInfo("Starting battery analytics service");
        _collectionTimer.Start();
    }

    /// <summary>
    /// Stops the analytics data collection.
    /// </summary>
    public void Stop()
    {
        _logger.LogInfo("Stopping battery analytics service");
        _collectionTimer.Stop();

        // End any active charging session
        if (_currentChargingSession != null)
        {
            EndChargingSession();
        }
    }

    /// <summary>
    /// Records a battery reading from the current snapshot.
    /// </summary>
    public void RecordReading(BatterySnapshot snapshot)
    {
        try
        {
            if (snapshot == null || !snapshot.BatteryPercentage.HasValue)
            {
                _logger.LogWarning("Cannot record reading: no battery percentage available");
                return;
            }

            var reading = new BatteryReading
            {
                Timestamp = DateTime.UtcNow,
                BatteryPercentage = snapshot.BatteryPercentage.Value,
                IsCharging = snapshot.IsCharging,
                IsAcConnected = snapshot.IsAcPowerConnected,
                IsBatteryAvailable = snapshot.IsBatteryAvailable
            };

            InsertBatteryReading(reading);
            TrackChargingSession(snapshot);
            _lastSnapshot = snapshot;

            _logger.LogDebug($"Recorded battery reading: {snapshot.BatteryPercentage}% (Charging: {snapshot.IsCharging})");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to record battery reading", ex);
        }
    }

    private void InsertBatteryReading(BatteryReading reading)
    {
        using var connection = _database.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO BatteryReadings (timestamp, battery_percentage, is_charging, is_ac_connected, is_battery_available)
            VALUES (@timestamp, @battery_percentage, @is_charging, @is_ac_connected, @is_battery_available)
        ";

        command.Parameters.AddWithValue("@timestamp", reading.Timestamp.ToUniversalTime().ToString("o"));
        command.Parameters.AddWithValue("@battery_percentage", reading.BatteryPercentage);
        command.Parameters.AddWithValue("@is_charging", reading.IsCharging ? 1 : 0);
        command.Parameters.AddWithValue("@is_ac_connected", reading.IsAcConnected ? 1 : 0);
        command.Parameters.AddWithValue("@is_battery_available", reading.IsBatteryAvailable ? 1 : 0);

        command.ExecuteNonQuery();
    }

    private void TrackChargingSession(BatterySnapshot snapshot)
    {
        // Start new charging session if not charging before and charging now
        if (_lastSnapshot != null && !_lastSnapshot.IsCharging && snapshot.IsCharging)
        {
            StartChargingSession(snapshot);
        }

        // End charging session if charging before and not charging now
        if (_lastSnapshot != null && _lastSnapshot.IsCharging && !snapshot.IsCharging)
        {
            EndChargingSession(snapshot);
        }

        // Track overcharge if charging and above target
        if (snapshot.IsCharging && snapshot.BatteryPercentage.HasValue)
        {
            TrackOvercharge(snapshot.BatteryPercentage.Value);
        }
        else
        {
            _overchargeStartTime = null;
        }
    }

    private void StartChargingSession(BatterySnapshot snapshot)
    {
        _logger.LogInfo($"Starting charging session at {snapshot.BatteryPercentage}%");

        _currentChargingSession = new ChargingSession
        {
            StartTime = DateTime.UtcNow,
            StartPercentage = snapshot.BatteryPercentage ?? 0,
            TargetPercentage = _settings.NormalTargetPercentage
        };

        using var connection = _database.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO ChargingSessions (start_time, start_percentage, target_percentage)
            VALUES (@start_time, @start_percentage, @target_percentage);
            SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("@start_time", _currentChargingSession.StartTime.ToUniversalTime().ToString("o"));
        command.Parameters.AddWithValue("@start_percentage", _currentChargingSession.StartPercentage);
        command.Parameters.AddWithValue("@target_percentage", _currentChargingSession.TargetPercentage);

        var result = command.ExecuteScalar();
        _currentChargingSession.Id = Convert.ToInt32(result);
    }

    private void EndChargingSession(BatterySnapshot? snapshot = null)
    {
        if (_currentChargingSession == null)
            return;

        var endPercentage = snapshot?.BatteryPercentage ?? _lastSnapshot?.BatteryPercentage;
        var endTime = DateTime.UtcNow;

        _logger.LogInfo($"Ending charging session at {endPercentage}%");

        _currentChargingSession.EndTime = endTime;
        _currentChargingSession.EndPercentage = endPercentage;
        _currentChargingSession.DurationMinutes = (endTime - _currentChargingSession.StartTime).TotalMinutes;

        // Check if overcharged
        if (_overchargeStartTime.HasValue)
        {
            _currentChargingSession.WasOvercharged = true;
            _currentChargingSession.OverchargeDurationMinutes = (endTime - _overchargeStartTime.Value).TotalMinutes;
        }

        using var connection = _database.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            UPDATE ChargingSessions
            SET end_time = @end_time,
                end_percentage = @end_percentage,
                duration_minutes = @duration_minutes,
                was_overcharged = @was_overcharged,
                overcharge_duration_minutes = @overcharge_duration_minutes
            WHERE id = @id
        ";

        command.Parameters.AddWithValue("@end_time", _currentChargingSession.EndTime.Value.ToUniversalTime().ToString("o"));
        command.Parameters.AddWithValue("@end_percentage", _currentChargingSession.EndPercentage);
        command.Parameters.AddWithValue("@duration_minutes", _currentChargingSession.DurationMinutes);
        command.Parameters.AddWithValue("@was_overcharged", _currentChargingSession.WasOvercharged ? 1 : 0);
        command.Parameters.AddWithValue("@overcharge_duration_minutes", _currentChargingSession.OverchargeDurationMinutes);
        command.Parameters.AddWithValue("@id", _currentChargingSession.Id);

        command.ExecuteNonQuery();

        _currentChargingSession = null;
        _overchargeStartTime = null;
    }

    private void TrackOvercharge(int currentPercentage)
    {
        if (currentPercentage > _settings.NormalTargetPercentage)
        {
            if (!_overchargeStartTime.HasValue)
            {
                _overchargeStartTime = DateTime.UtcNow;
                _logger.LogInfo($"Overcharge detected at {currentPercentage}% (target: {_settings.NormalTargetPercentage}%)");
            }
        }
        else
        {
            _overchargeStartTime = null;
        }
    }

    private void OnCollectionTimerTick(object? sender, EventArgs e)
    {
        // Timer-based collection is handled by battery state changes
        // This is a placeholder for periodic collection if needed
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _collectionTimer?.Stop();
            _collectionTimer?.Dispose();
            EndChargingSession();
            _disposed = true;
        }
    }
}