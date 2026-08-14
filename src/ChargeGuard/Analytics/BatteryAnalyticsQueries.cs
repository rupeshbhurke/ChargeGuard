using Microsoft.Data.Sqlite;

namespace ChargeGuard.Analytics;

/// <summary>
/// Provides query methods for battery analytics data.
/// </summary>
public class BatteryAnalyticsQueries
{
    private readonly BatteryDatabase _database;

    public BatteryAnalyticsQueries(BatteryDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    /// <summary>
    /// Gets battery readings for a specific date range.
    /// </summary>
    public List<BatteryReading> GetBatteryReadings(DateTime startDate, DateTime endDate)
    {
        var readings = new List<BatteryReading>();

        using var connection = _database.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT timestamp, battery_percentage, is_charging, is_ac_connected, is_battery_available
            FROM BatteryReadings
            WHERE timestamp >= @start_date AND timestamp <= @end_date
            ORDER BY timestamp ASC
        ";

        command.Parameters.AddWithValue("@start_date", startDate.ToUniversalTime().ToString("o"));
        command.Parameters.AddWithValue("@end_date", endDate.ToUniversalTime().ToString("o"));

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            readings.Add(new BatteryReading
            {
                Timestamp = DateTime.Parse(reader.GetString(0)),
                BatteryPercentage = reader.GetInt32(1),
                IsCharging = reader.GetBoolean(2),
                IsAcConnected = reader.GetBoolean(3),
                IsBatteryAvailable = reader.GetBoolean(4)
            });
        }

        return readings;
    }

    /// <summary>
    /// Gets charging sessions for a specific date range.
    /// </summary>
    public List<ChargingSession> GetChargingSessions(DateTime startDate, DateTime endDate)
    {
        var sessions = new List<ChargingSession>();

        using var connection = _database.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT id, start_time, end_time, start_percentage, end_percentage,
                   duration_minutes, was_overcharged, overcharge_duration_minutes,
                   target_percentage, notes
            FROM ChargingSessions
            WHERE date(start_time) >= date(@start_date) AND date(start_time) <= date(@end_date)
            ORDER BY start_time DESC
        ";

        command.Parameters.AddWithValue("@start_date", startDate.ToUniversalTime().ToString("o"));
        command.Parameters.AddWithValue("@end_date", endDate.ToUniversalTime().ToString("o"));

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            sessions.Add(new ChargingSession
            {
                Id = reader.GetInt32(0),
                StartTime = DateTime.Parse(reader.GetString(1)),
                EndTime = reader.IsDBNull(2) ? null : DateTime.Parse(reader.GetString(2)),
                StartPercentage = reader.GetInt32(3),
                EndPercentage = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                DurationMinutes = reader.IsDBNull(5) ? null : reader.GetDouble(5),
                WasOvercharged = reader.GetBoolean(6),
                OverchargeDurationMinutes = reader.IsDBNull(7) ? 0 : reader.GetDouble(7),
                TargetPercentage = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                Notes = reader.IsDBNull(9) ? null : reader.GetString(9)
            });
        }

        return sessions;
    }

    /// <summary>
    /// Gets statistics for a specific date range.
    /// </summary>
    public BatteryStatistics GetStatistics(DateTime startDate, DateTime endDate)
    {
        using var connection = _database.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT
                COUNT(*) as total_sessions,
                AVG(duration_minutes) as avg_duration,
                SUM(CASE WHEN was_overcharged = 1 THEN 1 ELSE 0 END) as overcharge_count,
                AVG(CASE WHEN was_overcharged = 1 THEN overcharge_duration_minutes ELSE 0 END) as avg_overcharge_duration,
                MIN(start_percentage) as min_start_percentage,
                MAX(end_percentage) as max_end_percentage
            FROM ChargingSessions
            WHERE date(start_time) >= date(@start_date) AND date(start_time) <= date(@end_date)
        ";

        command.Parameters.AddWithValue("@start_date", startDate.ToUniversalTime().ToString("o"));
        command.Parameters.AddWithValue("@end_date", endDate.ToUniversalTime().ToString("o"));

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var totalSessions = reader.GetInt32(0);
            
            // If no sessions, return empty statistics
            if (totalSessions == 0)
            {
                return new BatteryStatistics();
            }

            return new BatteryStatistics
            {
                TotalChargingSessions = totalSessions,
                AverageChargeDurationMinutes = reader.IsDBNull(1) ? 0 : reader.GetDouble(1),
                OverchargeCount = reader.GetInt32(2),
                AverageOverchargeDurationMinutes = reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
                MinStartPercentage = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                MaxEndPercentage = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
            };
        }

        return new BatteryStatistics();
    }

    /// <summary>
    /// Gets daily statistics for a specific date range.
    /// </summary>
    public List<DailyStatistics> GetDailyStatistics(DateTime startDate, DateTime endDate)
    {
        var stats = new List<DailyStatistics>();

        using var connection = _database.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT
                date,
                total_charging_time_minutes,
                total_discharging_time_minutes,
                number_of_charging_sessions,
                average_charge_time_minutes,
                average_discharge_time_minutes,
                max_battery_percentage,
                min_battery_percentage
            FROM DailyStats
            WHERE date >= date(@start_date) AND date <= date(@end_date)
            ORDER BY date ASC
        ";

        command.Parameters.AddWithValue("@start_date", startDate.ToUniversalTime().ToString("o"));
        command.Parameters.AddWithValue("@end_date", endDate.ToUniversalTime().ToString("o"));

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            stats.Add(new DailyStatistics
            {
                Date = DateTime.Parse(reader.GetString(0)),
                TotalChargingTimeMinutes = reader.IsDBNull(1) ? 0 : reader.GetDouble(1),
                TotalDischargingTimeMinutes = reader.IsDBNull(2) ? 0 : reader.GetDouble(2),
                ChargingReadingsCount = reader.GetInt32(3),
                AverageChargingTimeMinutes = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                AverageDischargingTimeMinutes = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                MaxLevel = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                MinLevel = reader.IsDBNull(7) ? 0 : reader.GetInt32(7)
            });
        }

        return stats;
    }

    /// <summary>
    /// Gets all discharge readings (is_charging = 0) for a specific date range.
    /// </summary>
    public List<BatteryReading> GetDischargeReadings(DateTime startDate, DateTime endDate)
    {
        var readings = new List<BatteryReading>();

        using var connection = _database.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT timestamp, battery_percentage, is_charging, is_ac_connected, is_battery_available
            FROM BatteryReadings
            WHERE is_charging = 0
              AND timestamp >= @start_date AND timestamp <= @end_date
            ORDER BY timestamp ASC
        ";

        command.Parameters.AddWithValue("@start_date", startDate.ToUniversalTime().ToString("o"));
        command.Parameters.AddWithValue("@end_date", endDate.ToUniversalTime().ToString("o"));

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            readings.Add(new BatteryReading
            {
                Timestamp = DateTime.Parse(reader.GetString(0)),
                BatteryPercentage = reader.GetInt32(1),
                IsCharging = reader.GetBoolean(2),
                IsAcConnected = reader.GetBoolean(3),
                IsBatteryAvailable = reader.GetBoolean(4)
            });
        }

        return readings;
    }

    /// <summary>
    /// Identifies continuous discharge sessions from battery readings.
    /// A discharge session is a consecutive run of readings where is_charging = 0.
    /// </summary>
    public List<DischargeSession> GetDischargeSessions(DateTime startDate, DateTime endDate)
    {
        var allReadings = GetBatteryReadings(startDate, endDate);
        var sessions = new List<DischargeSession>();
        DischargeSession? current = null;
        BatteryReading? prevDischargeReading = null;

        foreach (var reading in allReadings)
        {
            if (!reading.IsCharging)
            {
                if (current == null)
                {
                    current = new DischargeSession
                    {
                        StartTime = reading.Timestamp,
                        StartPercentage = reading.BatteryPercentage
                    };
                    prevDischargeReading = reading;
                }
                else
                {
                    prevDischargeReading = reading;
                }
            }
            else
            {
                if (current != null && prevDischargeReading != null)
                {
                    current.EndTime = prevDischargeReading.Timestamp;
                    current.EndPercentage = prevDischargeReading.BatteryPercentage;
                    current.DurationMinutes = (current.EndTime.Value - current.StartTime).TotalMinutes;
                    current.DropPercentage = current.StartPercentage - current.EndPercentage.Value;
                    current.RatePerHour = current.DurationMinutes > 0
                        ? current.DropPercentage / (current.DurationMinutes / 60.0)
                        : 0;
                    sessions.Add(current);
                    current = null;
                    prevDischargeReading = null;
                }
            }
        }

        if (current != null && prevDischargeReading != null)
        {
            current.EndTime = prevDischargeReading.Timestamp;
            current.EndPercentage = prevDischargeReading.BatteryPercentage;
            current.DurationMinutes = (current.EndTime.Value - current.StartTime).TotalMinutes;
            current.DropPercentage = current.StartPercentage - current.EndPercentage.Value;
            current.RatePerHour = current.DurationMinutes > 0
                ? current.DropPercentage / (current.DurationMinutes / 60.0)
                : 0;
            sessions.Add(current);
        }

        return sessions;
    }

    /// <summary>
    /// Gets hourly discharge data for heatmap visualization.
    /// Groups discharge readings by hour-of-day (0-23) and returns average level + count.
    /// </summary>
    public List<HourlyDischargeData> GetHourlyDischargeData(DateTime startDate, DateTime endDate)
    {
        var result = new List<HourlyDischargeData>();

        using var connection = _database.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT
                CAST(strftime('%H', timestamp) AS INTEGER) as hour_of_day,
                AVG(battery_percentage) as avg_level,
                COUNT(*) as reading_count
            FROM BatteryReadings
            WHERE is_charging = 0
              AND timestamp >= @start_date AND timestamp <= @end_date
            GROUP BY hour_of_day
            ORDER BY hour_of_day ASC
        ";

        command.Parameters.AddWithValue("@start_date", startDate.ToUniversalTime().ToString("o"));
        command.Parameters.AddWithValue("@end_date", endDate.ToUniversalTime().ToString("o"));

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new HourlyDischargeData
            {
                HourOfDay = reader.GetInt32(0),
                AverageLevel = reader.IsDBNull(1) ? 0 : reader.GetDouble(1),
                ReadingCount = reader.GetInt32(2)
            });
        }

        return result;
    }

    /// <summary>
    /// Gets daily charge vs discharge summary for comparison charts.
    /// </summary>
    public List<ChargeDischargeDaySummary> GetChargeDischargeSummary(DateTime startDate, DateTime endDate)
    {
        var result = new List<ChargeDischargeDaySummary>();

        using var connection = _database.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT
                date(timestamp) as day,
                COUNT(CASE WHEN is_charging = 1 THEN 1 END) as charge_count,
                COUNT(CASE WHEN is_charging = 0 THEN 1 END) as discharge_count,
                AVG(CASE WHEN is_charging = 1 THEN battery_percentage END) as avg_charge_level,
                AVG(CASE WHEN is_charging = 0 THEN battery_percentage END) as avg_discharge_level,
                MIN(battery_percentage) as min_level,
                MAX(battery_percentage) as max_level
            FROM BatteryReadings
            WHERE date(timestamp) >= date(@start_date) AND date(timestamp) <= date(@end_date)
            GROUP BY date(timestamp)
            ORDER BY day ASC
        ";

        command.Parameters.AddWithValue("@start_date", startDate.ToUniversalTime().ToString("o"));
        command.Parameters.AddWithValue("@end_date", endDate.ToUniversalTime().ToString("o"));

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var chargeCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            var dischargeCount = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

            result.Add(new ChargeDischargeDaySummary
            {
                Date = DateTime.Parse(reader.GetString(0)),
                ChargeTimeMinutes = chargeCount * 5.0,
                DischargeTimeMinutes = dischargeCount * 5.0,
                AverageChargeLevel = reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
                AverageDischargeLevel = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                MinLevel = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                MaxLevel = reader.IsDBNull(6) ? 0 : reader.GetInt32(6)
            });
        }

        return result;
    }
}

/// <summary>
/// Statistics for battery performance over a period.
/// </summary>
public class BatteryStatistics
{
    public int TotalChargingSessions { get; set; }
    public double AverageChargeDurationMinutes { get; set; }
    public int OverchargeCount { get; set; }
    public double AverageOverchargeDurationMinutes { get; set; }
    public int MinStartPercentage { get; set; }
    public int MaxEndPercentage { get; set; }
}

/// <summary>
/// Daily battery statistics.
/// </summary>
public class DailyStatistics
{
    public DateTime Date { get; set; }
    public double AverageChargeLevel { get; set; }
    public double AverageDischargeLevel { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public int ChargingReadingsCount { get; set; }
    public int DischargingReadingsCount { get; set; }
    public double TotalChargingTimeMinutes { get; set; }
    public double TotalDischargingTimeMinutes { get; set; }
    public double AverageChargingTimeMinutes { get; set; }
    public double AverageDischargingTimeMinutes { get; set; }
}

/// <summary>
/// Represents a continuous discharge session between two charging periods.
/// </summary>
public class DischargeSession
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int StartPercentage { get; set; }
    public int? EndPercentage { get; set; }
    public double DurationMinutes { get; set; }
    public int DropPercentage { get; set; }
    public double RatePerHour { get; set; }
}

/// <summary>
/// Hourly discharge data for heatmap visualization.
/// </summary>
public class HourlyDischargeData
{
    public int HourOfDay { get; set; }
    public double AverageLevel { get; set; }
    public int ReadingCount { get; set; }
}

/// <summary>
/// Daily charge vs discharge summary for comparison charts.
/// </summary>
public class ChargeDischargeDaySummary
{
    public DateTime Date { get; set; }
    public double ChargeTimeMinutes { get; set; }
    public double DischargeTimeMinutes { get; set; }
    public double AverageChargeLevel { get; set; }
    public double AverageDischargeLevel { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
}