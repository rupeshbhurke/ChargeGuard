using System.Windows.Forms;
using ChargeGuard.Analytics;
using ChargeGuard.Logging;

namespace ChargeGuard.UI;

/// <summary>
/// Window for displaying battery analytics and statistics.
/// </summary>
public partial class AnalyticsWindow : Form
{
    private readonly BatteryAnalyticsQueries _queries;
    private readonly IAppLogger _logger;
    private DateTime _startDate;
    private DateTime _endDate;

    // UI Controls
    private DateTimePicker _startDatePicker = null!;
    private DateTimePicker _endDatePicker = null!;
    private Button _refreshButton = null!;
    private TabControl _tabControl = null!;
    private TabPage _summaryTab = null!;
    private TabPage _sessionsTab = null!;
    private TabPage _readingsTab = null!;
    private Label _totalSessionsLabel = null!;
    private Label _avgDurationLabel = null!;
    private Label _overchargeCountLabel = null!;
    private Label _avgOverchargeDurationLabel = null!;
    private DataGridView _sessionsGrid = null!;
    private DataGridView _readingsGrid = null!;
    private Panel _batteryChartPanel = null!;
    private Panel _chargingChartPanel = null!;
    private int? _hoveredDataIndex = null!;

    public AnalyticsWindow(BatteryAnalyticsQueries queries, IAppLogger logger)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Default to last 7 days
        _endDate = DateTime.Today;
        _startDate = _endDate.AddDays(-7);

        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form properties
        this.Text = "📊 Battery Analytics";
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MinimumSize = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.ClientSize = new Size(900, 700);
        this.BackColor = Color.FromArgb(245, 245, 245);

        // Create header
        var headerPanel = CreateHeader();
        headerPanel.Location = new Point(0, 0);
        headerPanel.Size = new Size(this.ClientSize.Width, 80);

        // Create date range selector
        var dateRangePanel = CreateDateRangeSelector();
        dateRangePanel.Location = new Point(20, 100);
        dateRangePanel.Size = new Size(860, 50);

        // Create tab control
        _tabControl = new TabControl
        {
            Location = new Point(20, 160),
            Size = new Size(860, 500),
            Font = new Font("Segoe UI", 9)
        };

        // Create tabs
        _summaryTab = CreateSummaryTab();
        _sessionsTab = CreateSessionsTab();
        _readingsTab = CreateReadingsTab();

        _tabControl.TabPages.Add(_summaryTab);
        _tabControl.TabPages.Add(_sessionsTab);
        _tabControl.TabPages.Add(_readingsTab);

        // Add controls
        this.Controls.Add(headerPanel);
        this.Controls.Add(dateRangePanel);
        this.Controls.Add(_tabControl);

        this.ResumeLayout(false);
    }

    private Panel CreateHeader()
    {
        var panel = new Panel
        {
            BackColor = Color.FromArgb(0, 120, 215),
            Dock = DockStyle.Top
        };

        var titleLabel = new Label
        {
            Text = "📊 Battery Analytics",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 20),
            AutoSize = true
        };

        var subtitleLabel = new Label
        {
            Text = "Track battery performance, charging patterns, and health",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(200, 200, 200),
            Location = new Point(20, 50),
            AutoSize = true
        };

        panel.Controls.Add(titleLabel);
        panel.Controls.Add(subtitleLabel);

        return panel;
    }

    private Panel CreateDateRangeSelector()
    {
        var panel = new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var startDateLabel = new Label
        {
            Text = "From:",
            Location = new Point(15, 15),
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };

        _startDatePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Location = new Point(60, 12),
            Size = new Size(120, 23),
            Value = _startDate,
            Font = new Font("Segoe UI", 9)
        };

        var endDateLabel = new Label
        {
            Text = "To:",
            Location = new Point(200, 15),
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };

        _endDatePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Location = new Point(240, 12),
            Size = new Size(120, 23),
            Value = _endDate,
            Font = new Font("Segoe UI", 9)
        };

        _refreshButton = new Button
        {
            Text = "🔄 Refresh",
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(380, 10),
            Size = new Size(100, 28),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        _refreshButton.FlatAppearance.BorderSize = 0;
        _refreshButton.Click += OnRefreshClick;

        panel.Controls.Add(startDateLabel);
        panel.Controls.Add(_startDatePicker);
        panel.Controls.Add(endDateLabel);
        panel.Controls.Add(_endDatePicker);
        panel.Controls.Add(_refreshButton);

        return panel;
    }

    private TabPage CreateSummaryTab()
    {
        var tab = new TabPage("📈 Summary");
        tab.BackColor = Color.White;

        var statisticsGroup = new GroupBox
        {
            Text = "📊 Statistics",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            Location = new Point(20, 20),
            Size = new Size(800, 120)
        };

        _totalSessionsLabel = CreateModernLabel("Total Charging Sessions: --", Color.FromArgb(0, 0, 0));
        _avgDurationLabel = CreateModernLabel("Average Charge Duration: --", Color.FromArgb(0, 0, 0));
        _overchargeCountLabel = CreateModernLabel("Overcharge Events: --", Color.FromArgb(200, 50, 50));
        _avgOverchargeDurationLabel = CreateModernLabel("Average Overcharge Duration: --", Color.FromArgb(200, 50, 50));

        statisticsGroup.Controls.Add(_totalSessionsLabel);
        statisticsGroup.Controls.Add(_avgDurationLabel);
        statisticsGroup.Controls.Add(_overchargeCountLabel);
        statisticsGroup.Controls.Add(_avgOverchargeDurationLabel);

        _totalSessionsLabel.Location = new Point(20, 30);
        _avgDurationLabel.Location = new Point(20, 60);
        _overchargeCountLabel.Location = new Point(400, 30);
        _avgOverchargeDurationLabel.Location = new Point(400, 60);

        // Battery percentage chart panel
        var batteryChartGroup = new GroupBox
        {
            Text = "📉 Battery Percentage Over Time",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            Location = new Point(20, 150),
            Size = new Size(380, 280)
        };

        _batteryChartPanel = new Panel
        {
            Location = new Point(10, 25),
            Size = new Size(360, 240),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _batteryChartPanel.Paint += OnBatteryChartPaint;
        _batteryChartPanel.MouseClick += OnBatteryChartClick;
        _batteryChartPanel.MouseMove += OnBatteryChartMouseMove;
        _batteryChartPanel.MouseLeave += OnChartMouseLeave;

        batteryChartGroup.Controls.Add(_batteryChartPanel);

        // Charging pattern chart panel
        var chargingChartGroup = new GroupBox
        {
            Text = "🔌 Charging Pattern",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            Location = new Point(420, 150),
            Size = new Size(380, 280)
        };

        _chargingChartPanel = new Panel
        {
            Location = new Point(10, 25),
            Size = new Size(360, 240),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _chargingChartPanel.Paint += OnChargingChartPaint;
        _chargingChartPanel.MouseClick += OnChargingChartClick;
        _chargingChartPanel.MouseMove += OnChargingChartMouseMove;
        _chargingChartPanel.MouseLeave += OnChartMouseLeave;

        chargingChartGroup.Controls.Add(_chargingChartPanel);

        tab.Controls.Add(statisticsGroup);
        tab.Controls.Add(batteryChartGroup);
        tab.Controls.Add(chargingChartGroup);

        return tab;
    }

    private TabPage CreateSessionsTab()
    {
        var tab = new TabPage("🔌 Charging Sessions");
        tab.BackColor = Color.White;

        _sessionsGrid = new DataGridView
        {
            Location = new Point(20, 20),
            Size = new Size(800, 400),
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Font = new Font("Segoe UI", 9)
        };

        _sessionsGrid.Columns.Add("StartTime", "Start Time");
        _sessionsGrid.Columns.Add("EndTime", "End Time");
        _sessionsGrid.Columns.Add("StartPercentage", "Start %");
        _sessionsGrid.Columns.Add("EndPercentage", "End %");
        _sessionsGrid.Columns.Add("Duration", "Duration (min)");
        _sessionsGrid.Columns.Add("WasOvercharged", "Overcharged");
        _sessionsGrid.Columns.Add("OverchargeDuration", "Overcharge (min)");
        _sessionsGrid.CellDoubleClick += OnSessionGridDoubleClick;

        tab.Controls.Add(_sessionsGrid);

        return tab;
    }

    private TabPage CreateReadingsTab()
    {
        var tab = new TabPage("📉 Battery Readings");
        tab.BackColor = Color.White;

        _readingsGrid = new DataGridView
        {
            Location = new Point(20, 20),
            Size = new Size(800, 400),
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Font = new Font("Segoe UI", 9)
        };

        _readingsGrid.Columns.Add("Timestamp", "Timestamp");
        _readingsGrid.Columns.Add("BatteryPercentage", "Battery %");
        _readingsGrid.Columns.Add("IsCharging", "Charging");
        _readingsGrid.Columns.Add("IsAcConnected", "AC Connected");

        tab.Controls.Add(_readingsGrid);

        return tab;
    }

    private Label CreateModernLabel(string text, Color color)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = color,
            AutoSize = true
        };
    }

    private void OnRefreshClick(object? sender, EventArgs e)
    {
        _startDate = _startDatePicker.Value.Date;
        _endDate = _endDatePicker.Value.Date.AddDays(1).AddTicks(-1); // End of day
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            _logger.LogInfo($"Loading analytics data from {_startDate:yyyy-MM-dd} to {_endDate:yyyy-MM-dd}");

            // Load statistics
            var stats = _queries.GetStatistics(_startDate, _endDate);
            _totalSessionsLabel.Text = $"Total Charging Sessions: {stats.TotalChargingSessions}";
            _avgDurationLabel.Text = $"Average Charge Duration: {stats.AverageChargeDurationMinutes:F1} minutes";
            _overchargeCountLabel.Text = $"Overcharge Events: {stats.OverchargeCount}";
            _avgOverchargeDurationLabel.Text = $"Average Overcharge Duration: {stats.AverageOverchargeDurationMinutes:F1} minutes";

            // Load charging sessions
            var sessions = _queries.GetChargingSessions(_startDate, _endDate);
            _sessionsGrid.Rows.Clear();
            foreach (var session in sessions)
            {
                _sessionsGrid.Rows.Add(
                    session.StartTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    session.EndTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "Active",
                    session.StartPercentage,
                    session.EndPercentage?.ToString() ?? "--",
                    session.DurationMinutes?.ToString("F1") ?? "--",
                    session.WasOvercharged ? "Yes" : "No",
                    session.OverchargeDurationMinutes > 0 ? session.OverchargeDurationMinutes.ToString("F1") : "0"
                );
            }

            // Load battery readings (limit to last 100 for performance)
            var readings = _queries.GetBatteryReadings(_startDate, _endDate).Take(100).ToList();
            _currentReadings = readings;
            _readingsGrid.Rows.Clear();
            foreach (var reading in readings)
            {
                _readingsGrid.Rows.Add(
                    reading.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    reading.BatteryPercentage,
                    reading.IsCharging ? "Yes" : "No",
                    reading.IsAcConnected ? "Yes" : "No"
                );
            }

            // Update charts
            _batteryChartPanel.Invalidate();
            _chargingChartPanel.Invalidate();

            _logger.LogInfo($"Analytics data loaded: {sessions.Count} sessions, {readings.Count} readings");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load analytics data", ex);
            MessageBox.Show($"Failed to load analytics data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private List<BatteryReading> _currentReadings = new List<BatteryReading>();

    private void OnBatteryChartPaint(object? sender, PaintEventArgs e)
    {
        DrawBatteryPercentageChart(e.Graphics, _batteryChartPanel.ClientSize);
    }

    private void OnChargingChartPaint(object? sender, PaintEventArgs e)
    {
        DrawChargingPatternChart(e.Graphics, _chargingChartPanel.ClientSize);
    }

    private void DrawBatteryPercentageChart(Graphics g, Size size)
    {
        g.Clear(Color.White);

        if (_currentReadings.Count == 0)
        {
            DrawNoDataMessage(g, size, "No data available");
            return;
        }

        var padding = 40;
        var chartArea = new Rectangle(padding, padding, size.Width - 2 * padding, size.Height - 2 * padding);

        // Draw axes
        g.DrawLine(Pens.Black, chartArea.Left, chartArea.Bottom, chartArea.Right, chartArea.Bottom);
        g.DrawLine(Pens.Black, chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom);

        // Draw Y-axis labels (0-100%)
        for (int i = 0; i <= 100; i += 20)
        {
            var y = chartArea.Bottom - (i / 100.0) * chartArea.Height;
            g.DrawLine(Pens.LightGray, chartArea.Left, (int)y, chartArea.Right, (int)y);
            g.DrawString(i.ToString(), new Font("Segoe UI", 8), Brushes.Black, 5, (int)y - 6);
        }

        // Draw battery percentage line
        if (_currentReadings.Count > 1)
        {
            var points = new List<Point>();
            var minX = _currentReadings.Min(r => r.Timestamp);
            var maxX = _currentReadings.Max(r => r.Timestamp);
            var timeSpan = (maxX - minX).TotalMinutes;

            foreach (var reading in _currentReadings)
            {
                var x = timeSpan > 0 
                    ? chartArea.Left + (reading.Timestamp - minX).TotalMinutes / timeSpan * chartArea.Width 
                    : chartArea.Left + chartArea.Width / 2;
                var y = chartArea.Bottom - (reading.BatteryPercentage / 100.0) * chartArea.Height;
                points.Add(new Point((int)x, (int)y));
            }

            if (points.Count > 1)
            {
                g.DrawLines(new Pen(Color.FromArgb(0, 120, 215), 2), points.ToArray());
            }

            // Draw hover indicator
            if (_hoveredDataIndex.HasValue && _hoveredDataIndex.Value < points.Count)
            {
                var hoverPoint = points[_hoveredDataIndex.Value];
                g.FillEllipse(Brushes.Red, hoverPoint.X - 5, hoverPoint.Y - 5, 10, 10);
                g.DrawEllipse(Pens.Red, hoverPoint.X - 5, hoverPoint.Y - 5, 10, 10);
            }
        }

        // Draw title
        g.DrawString("Battery %", new Font("Segoe UI", 8, FontStyle.Bold), Brushes.Black, 5, 5);
    }

    private void DrawChargingPatternChart(Graphics g, Size size)
    {
        g.Clear(Color.White);

        if (_currentReadings.Count == 0)
        {
            DrawNoDataMessage(g, size, "No data available");
            return;
        }

        var padding = 40;
        var chartArea = new Rectangle(padding, padding, size.Width - 2 * padding, size.Height - 2 * padding);

        // Draw axes
        g.DrawLine(Pens.Black, chartArea.Left, chartArea.Bottom, chartArea.Right, chartArea.Bottom);
        g.DrawLine(Pens.Black, chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom);

        // Draw Y-axis labels (0-100%)
        for (int i = 0; i <= 100; i += 20)
        {
            var y = chartArea.Bottom - (i / 100.0) * chartArea.Height;
            g.DrawLine(Pens.LightGray, chartArea.Left, (int)y, chartArea.Right, (int)y);
            g.DrawString(i.ToString(), new Font("Segoe UI", 8), Brushes.Black, 5, (int)y - 6);
        }

        // Group by date and calculate averages
        var dailyData = _currentReadings
            .GroupBy(r => r.Timestamp.Date)
            .Select(g => new
            {
                Date = g.Key,
                AvgChargeLevel = g.Where(r => r.IsCharging).Any() ? g.Where(r => r.IsCharging).Average(r => r.BatteryPercentage) : 0,
                AvgDischargeLevel = g.Where(r => !r.IsCharging).Any() ? g.Where(r => !r.IsCharging).Average(r => r.BatteryPercentage) : 0
            })
            .OrderBy(d => d.Date)
            .ToList();

        if (dailyData.Count > 0)
        {
            var minX = dailyData.Min(d => d.Date);
            var maxX = dailyData.Max(d => d.Date);
            var dateSpan = (maxX - minX).TotalDays;

            // Draw charging points (green)
            var chargePoints = new List<Point>();
            foreach (var data in dailyData)
            {
                if (data.AvgChargeLevel > 0)
                {
                    var x = dateSpan > 0 
                        ? chartArea.Left + (data.Date - minX).TotalDays / dateSpan * chartArea.Width 
                        : chartArea.Left + chartArea.Width / 2;
                    var y = chartArea.Bottom - (data.AvgChargeLevel / 100.0) * chartArea.Height;
                    chargePoints.Add(new Point((int)x, (int)y));
                }
            }

            if (chargePoints.Count > 1)
            {
                g.DrawLines(new Pen(Color.FromArgb(40, 167, 69), 2), chargePoints.ToArray());
            }

            // Draw discharging points (red)
            var dischargePoints = new List<Point>();
            foreach (var data in dailyData)
            {
                if (data.AvgDischargeLevel > 0)
                {
                    var x = dateSpan > 0 
                        ? chartArea.Left + (data.Date - minX).TotalDays / dateSpan * chartArea.Width 
                        : chartArea.Left + chartArea.Width / 2;
                    var y = chartArea.Bottom - (data.AvgDischargeLevel / 100.0) * chartArea.Height;
                    dischargePoints.Add(new Point((int)x, (int)y));
                }
            }

            if (dischargePoints.Count > 1)
            {
                g.DrawLines(new Pen(Color.FromArgb(220, 53, 69), 2), dischargePoints.ToArray());
            }

            // Draw legend
            g.DrawString("● Charging", new Font("Segoe UI", 8), new SolidBrush(Color.FromArgb(40, 167, 69)), size.Width - 80, 5);
            g.DrawString("● Discharging", new Font("Segoe UI", 8), new SolidBrush(Color.FromArgb(220, 53, 69)), size.Width - 80, 20);

            // Draw hover indicator
            if (_hoveredDataIndex.HasValue && _hoveredDataIndex.Value < dailyData.Count)
            {
                var data = dailyData[_hoveredDataIndex.Value];
                var x = dateSpan > 0 
                    ? chartArea.Left + (data.Date - minX).TotalDays / dateSpan * chartArea.Width 
                    : chartArea.Left + chartArea.Width / 2;
                var y = data.AvgChargeLevel > 0 
                    ? chartArea.Bottom - (data.AvgChargeLevel / 100.0) * chartArea.Height 
                    : chartArea.Bottom - (data.AvgDischargeLevel / 100.0) * chartArea.Height;
                
                g.FillEllipse(Brushes.Red, (int)x - 5, (int)y - 5, 10, 10);
                g.DrawEllipse(Pens.Red, (int)x - 5, (int)y - 5, 10, 10);
            }
        }

        g.DrawString("Battery %", new Font("Segoe UI", 8, FontStyle.Bold), Brushes.Black, 5, 5);
    }

    private void DrawNoDataMessage(Graphics g, Size size, string message)
    {
        var textSize = g.MeasureString(message, new Font("Segoe UI", 10));
        var x = (size.Width - textSize.Width) / 2;
        var y = (size.Height - textSize.Height) / 2;
        g.DrawString(message, new Font("Segoe UI", 10), Brushes.Gray, (int)x, (int)y);
    }

    private void OnBatteryChartClick(object? sender, MouseEventArgs e)
    {
        if (_currentReadings.Count == 0) return;

        var clickedIndex = FindNearestDataPoint(e.Location, _batteryChartPanel.ClientSize, isBatteryChart: true);
        if (clickedIndex.HasValue && clickedIndex.Value < _currentReadings.Count)
        {
            var reading = _currentReadings[clickedIndex.Value];
            ShowReadingDetails(reading);
        }
    }

    private void OnChargingChartClick(object? sender, MouseEventArgs e)
    {
        if (_currentReadings.Count == 0) return;

        var clickedIndex = FindNearestDataPoint(e.Location, _chargingChartPanel.ClientSize, isBatteryChart: false);
        if (clickedIndex.HasValue && clickedIndex.Value < _currentReadings.Count)
        {
            var reading = _currentReadings[clickedIndex.Value];
            ShowReadingDetails(reading);
        }
    }

    private void OnBatteryChartMouseMove(object? sender, MouseEventArgs e)
    {
        if (_currentReadings.Count == 0) return;

        var nearestIndex = FindNearestDataPoint(e.Location, _batteryChartPanel.ClientSize, isBatteryChart: true);
        if (nearestIndex.HasValue && nearestIndex.Value < _currentReadings.Count)
        {
            _hoveredDataIndex = nearestIndex.Value;
            _batteryChartPanel.Invalidate();
            _batteryChartPanel.Cursor = Cursors.Hand;
        }
        else
        {
            _hoveredDataIndex = null;
            _batteryChartPanel.Invalidate();
            _batteryChartPanel.Cursor = Cursors.Default;
        }
    }

    private void OnChargingChartMouseMove(object? sender, MouseEventArgs e)
    {
        if (_currentReadings.Count == 0) return;

        var nearestIndex = FindNearestDataPoint(e.Location, _chargingChartPanel.ClientSize, isBatteryChart: false);
        if (nearestIndex.HasValue && nearestIndex.Value < _currentReadings.Count)
        {
            _hoveredDataIndex = nearestIndex.Value;
            _chargingChartPanel.Invalidate();
            _chargingChartPanel.Cursor = Cursors.Hand;
        }
        else
        {
            _hoveredDataIndex = null;
            _chargingChartPanel.Invalidate();
            _chargingChartPanel.Cursor = Cursors.Default;
        }
    }

    private void OnChartMouseLeave(object? sender, EventArgs e)
    {
        _hoveredDataIndex = null;
        if (sender is Panel panel)
        {
            panel.Invalidate();
            panel.Cursor = Cursors.Default;
        }
    }

    private int? FindNearestDataPoint(Point clickPoint, Size size, bool isBatteryChart)
    {
        if (_currentReadings.Count == 0) return null;

        var padding = 40;
        var chartArea = new Rectangle(padding, padding, size.Width - 2 * padding, size.Height - 2 * padding);

        if (isBatteryChart)
        {
            var minX = _currentReadings.Min(r => r.Timestamp);
            var maxX = _currentReadings.Max(r => r.Timestamp);
            var timeSpan = (maxX - minX).TotalMinutes;

            int? nearestIndex = null;
            double minDistance = double.MaxValue;

            for (int i = 0; i < _currentReadings.Count; i++)
            {
                var reading = _currentReadings[i];
                var x = timeSpan > 0 
                    ? chartArea.Left + (reading.Timestamp - minX).TotalMinutes / timeSpan * chartArea.Width 
                    : chartArea.Left + chartArea.Width / 2;
                var y = chartArea.Bottom - (reading.BatteryPercentage / 100.0) * chartArea.Height;

                var distance = Math.Sqrt(Math.Pow(clickPoint.X - x, 2) + Math.Pow(clickPoint.Y - y, 2));
                if (distance < minDistance && distance < 20) // 20 pixel threshold
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }
        else
        {
            // For charging pattern chart, find nearest daily data point
            var dailyData = _currentReadings
                .GroupBy(r => r.Timestamp.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    AvgChargeLevel = g.Where(r => r.IsCharging).Any() ? g.Where(r => r.IsCharging).Average(r => r.BatteryPercentage) : 0,
                    AvgDischargeLevel = g.Where(r => !r.IsCharging).Any() ? g.Where(r => !r.IsCharging).Average(r => r.BatteryPercentage) : 0
                })
                .OrderBy(d => d.Date)
                .ToList();

            if (dailyData.Count == 0) return null;

            var minX = dailyData.Min(d => d.Date);
            var maxX = dailyData.Max(d => d.Date);
            var dateSpan = (maxX - minX).TotalDays;

            int? nearestIndex = null;
            double minDistance = double.MaxValue;

            for (int i = 0; i < dailyData.Count; i++)
            {
                var data = dailyData[i];
                var x = dateSpan > 0 
                    ? chartArea.Left + (data.Date - minX).TotalDays / dateSpan * chartArea.Width 
                    : chartArea.Left + chartArea.Width / 2;
                var y = data.AvgChargeLevel > 0 
                    ? chartArea.Bottom - (data.AvgChargeLevel / 100.0) * chartArea.Height 
                    : chartArea.Bottom - (data.AvgDischargeLevel / 100.0) * chartArea.Height;

                var distance = Math.Sqrt(Math.Pow(clickPoint.X - x, 2) + Math.Pow(clickPoint.Y - y, 2));
                if (distance < minDistance && distance < 20)
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }
    }

    private void ShowReadingDetails(BatteryReading reading)
    {
        var details = $"Battery Reading Details\n\n" +
                     $"Timestamp: {reading.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss}\n" +
                     $"Battery Percentage: {reading.BatteryPercentage}%\n" +
                     $"Charging: {(reading.IsCharging ? "Yes" : "No")}\n" +
                     $"AC Connected: {(reading.IsAcConnected ? "Yes" : "No")}\n" +
                     $"Battery Available: {(reading.IsBatteryAvailable ? "Yes" : "No")}";

        MessageBox.Show(details, "Reading Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnSessionGridDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _sessionsGrid.Rows.Count) return;

        var row = _sessionsGrid.Rows[e.RowIndex];
        if (row.Cells.Count < 7) return;

        var startTime = row.Cells[0].Value?.ToString();
        var endTime = row.Cells[1].Value?.ToString();
        var startPercentage = row.Cells[2].Value?.ToString();
        var endPercentage = row.Cells[3].Value?.ToString();
        var duration = row.Cells[4].Value?.ToString();
        var wasOvercharged = row.Cells[5].Value?.ToString();
        var overchargeDuration = row.Cells[6].Value?.ToString();

        var details = $"Charging Session Details\n\n" +
                     $"Start Time: {startTime}\n" +
                     $"End Time: {endTime}\n" +
                     $"Start Percentage: {startPercentage}%\n" +
                     $"End Percentage: {endPercentage}%\n" +
                     $"Duration: {duration} minutes\n" +
                     $"Overcharged: {wasOvercharged}\n" +
                     $"Overcharge Duration: {overchargeDuration} minutes";

        MessageBox.Show(details, "Session Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}