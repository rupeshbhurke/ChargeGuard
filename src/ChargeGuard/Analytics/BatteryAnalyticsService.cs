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

        // Update daily stats
        UpdateDailyStats(snapshot);
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

        // Update daily stats to increment charging session count
        UpdateDailyStatsSessionCount();

        _currentChargingSession = null;
        _overchargeStartTime = null;
    }

    private void UpdateDailyStatsSessionCount()
    {
        try
        {
            var today = DateTime.UtcNow.Date;

            using var connection = _database.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE DailyStats
                SET number_of_charging_sessions = number_of_charging_sessions + 1
                WHERE date = @date
            ";
            command.Parameters.AddWithValue("@date", today.ToString("o"));

            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update daily stats session count", ex);
        }
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

    private void UpdateDailyStats(BatterySnapshot snapshot)
    {
        try
        {
            var today = DateTime.UtcNow.Date;

            using var connection = _database.GetConnection();
            using var command = connection.CreateCommand();

            // Check if daily stats exist for today
            command.CommandText = @"
                SELECT id, total_charging_time_minutes, total_discharging_time_minutes,
                       number_of_charging_sessions, max_battery_percentage, min_battery_percentage,
                       overcharge_minutes, number_of_overcharge_events
                FROM DailyStats
                WHERE date = @date
            ";
            command.Parameters.AddWithValue("@date", today.ToString("o"));

            double totalChargingTime = 0;
            double totalDischargingTime = 0;
            int chargingSessions = 0;
            int maxLevel = 0;
            int minLevel = 100;
            double overchargeMinutes = 0;
            int overchargeEvents = 0;
            int? existingId = null;

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                existingId = reader.GetInt32(0);
                totalChargingTime = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                totalDischargingTime = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                chargingSessions = reader.GetInt32(3);
                maxLevel = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                minLevel = reader.IsDBNull(5) ? 100 : reader.GetInt32(5);
                overchargeMinutes = reader.IsDBNull(6) ? 0 : reader.GetDouble(6);
                overchargeEvents = reader.GetInt32(7);
            }
            reader.Close();

            // Update values based on current state
            if (snapshot.BatteryPercentage.HasValue)
            {
                maxLevel = Math.Max(maxLevel, snapshot.BatteryPercentage.Value);
                minLevel = Math.Min(minLevel, snapshot.BatteryPercentage.Value);
            }

            // Add 5 minutes (collection interval) to appropriate time bucket
            if (snapshot.IsCharging)
            {
                totalChargingTime += 5;
            }
            else
            {
                totalDischargingTime += 5;
            }

            // Update overcharge tracking
            if (_overchargeStartTime.HasValue)
            {
                overchargeMinutes += 5;
                if (overchargeEvents == 0)
                {
                    overchargeEvents = 1;
                }
            }

            // Update or insert daily stats
            using var upsertCommand = connection.CreateCommand();
            if (existingId.HasValue)
            {
                upsertCommand.CommandText = @"
                    UPDATE DailyStats
                    SET total_charging_time_minutes = @total_charging_time,
                        total_discharging_time_minutes = @total_discharging_time,
                        number_of_charging_sessions = @charging_sessions,
                        max_battery_percentage = @max_level,
                        min_battery_percentage = @min_level,
                        overcharge_minutes = @overcharge_minutes,
                        number_of_overcharge_events = @overcharge_events,
                        average_charge_time_minutes = @avg_charge_time,
                        average_discharge_time_minutes = @avg_discharge_time
                    WHERE id = @id
                ";
                upsertCommand.Parameters.AddWithValue("@id", existingId.Value);
            }
            else
            {
                upsertCommand.CommandText = @"
                    INSERT INTO DailyStats (date, total_charging_time_minutes, total_discharging_time_minutes,
                                          number_of_charging_sessions, max_battery_percentage, min_battery_percentage,
                                          overcharge_minutes, number_of_overcharge_events,
                                          average_charge_time_minutes, average_discharge_time_minutes)
                    VALUES (@date, @total_charging_time, @total_discharging_time, @charging_sessions,
                            @max_level, @min_level, @overcharge_minutes, @overcharge_events,
                            @avg_charge_time, @avg_discharge_time)
                ";
                upsertCommand.Parameters.AddWithValue("@date", today.ToString("o"));
            }

            upsertCommand.Parameters.AddWithValue("@total_charging_time", totalChargingTime);
            upsertCommand.Parameters.AddWithValue("@total_discharging_time", totalDischargingTime);
            upsertCommand.Parameters.AddWithValue("@charging_sessions", chargingSessions);
            upsertCommand.Parameters.AddWithValue("@max_level", maxLevel);
            upsertCommand.Parameters.AddWithValue("@min_level", minLevel);
            upsertCommand.Parameters.AddWithValue("@overcharge_minutes", overchargeMinutes);
            upsertCommand.Parameters.AddWithValue("@overcharge_events", overchargeEvents);

            // Calculate averages
            double avgChargeTime = chargingSessions > 0 ? totalChargingTime / chargingSessions : 0;
            double avgDischargeTime = totalDischargingTime > 0 ? totalDischargingTime : 0; // No session count for discharge
            upsertCommand.Parameters.AddWithValue("@avg_charge_time", avgChargeTime);
            upsertCommand.Parameters.AddWithValue("@avg_discharge_time", avgDischargeTime);

            upsertCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update daily stats", ex);
        }
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