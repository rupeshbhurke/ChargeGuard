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
            Size = new Size(800, 200)
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

        tab.Controls.Add(statisticsGroup);

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

            _logger.LogInfo($"Analytics data loaded: {sessions.Count} sessions, {readings.Count} readings");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load analytics data", ex);
            MessageBox.Show($"Failed to load analytics data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}