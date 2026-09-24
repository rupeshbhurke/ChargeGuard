using System.Text.Json;
using System.Windows.Forms;
using ChargeGuard.Analytics;
using ChargeGuard.Logging;
using Microsoft.Web.WebView2.WinForms;

namespace ChargeGuard.UI;

/// <summary>
/// Window for displaying battery analytics via the web dashboard.
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
    private WebView2 _webView = null!;
    private bool _webViewReady = false;

    public AnalyticsWindow(BatteryAnalyticsQueries queries, IAppLogger logger)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Default to last 7 days
        _endDate = DateTime.Today;
        _startDate = _endDate.AddDays(-7);

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form properties
        this.Text = "📊 Battery Analytics";
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MinimumSize = new Size(600, 400);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.ClientSize = new Size(900, 700);
        this.BackColor = Color.FromArgb(245, 245, 245);

        // Create header
        var headerPanel = CreateHeader();

        // Create date range selector
        var dateRangePanel = CreateDateRangeSelector();

        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Color.White
        };

        // Add controls - order matters for docking (Fill goes first, then Top)
        this.Controls.Add(_webView);
        this.Controls.Add(dateRangePanel);
        this.Controls.Add(headerPanel);

        this.ResumeLayout(false);

        // Initialize WebView2 asynchronously
        _ = InitializeWebView2Async();
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
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Top,
            Height = 50
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

    private async Task InitializeWebView2Async()
    {
        try
        {
            await _webView.EnsureCoreWebView2Async(null);

            var dashboardPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Dashboard",
                "index.html");

            if (File.Exists(dashboardPath))
            {
                _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "chargeguard.dashboard",
                    Path.GetDirectoryName(dashboardPath)!,
                    Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

                _webView.CoreWebView2.Navigate($"https://chargeguard.dashboard/index.html");
            }
            else
            {
                _webView.CoreWebView2.NavigateToString(
                    "<html><body><h2>Dashboard files not found.</h2><p>Expected at: " +
                    dashboardPath.Replace("<", "&lt;").Replace(">", "&gt;") +
                    "</p></body></html>");
            }

            _webView.WebMessageReceived += OnWebViewMessageReceived;
            _webViewReady = true;

            SendDataToWebView();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to initialize WebView2", ex);
        }
    }

    private void OnWebViewMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.TryGetWebMessageAsString();
            if (message == "requestData")
            {
                SendDataToWebView();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling WebView message", ex);
        }
    }

    private void SendDataToWebView()
    {
        if (!_webViewReady || _webView.CoreWebView2 == null) return;

        try
        {
            var readings = _queries.GetBatteryReadings(_startDate, _endDate).Take(2000).ToList();
            var dischargeReadings = _queries.GetDischargeReadings(_startDate, _endDate);
            var dischargeSessions = _queries.GetDischargeSessions(_startDate, _endDate);
            var hourlyDischarge = _queries.GetHourlyDischargeData(_startDate, _endDate);
            var chargeDischargeSummary = _queries.GetChargeDischargeSummary(_startDate, _endDate);
            var stats = _queries.GetStatistics(_startDate, _endDate);

            var payload = new
            {
                batteryReadings = readings.Select(r => new
                {
                    timestamp = r.Timestamp.ToLocalTime().ToString("o"),
                    percentage = r.BatteryPercentage,
                    isCharging = r.IsCharging
                }),
                dischargeReadings = dischargeReadings.Select(r => new
                {
                    timestamp = r.Timestamp.ToLocalTime().ToString("o"),
                    percentage = r.BatteryPercentage
                }),
                dischargeSessions = dischargeSessions.Select(s => new
                {
                    startTime = s.StartTime.ToLocalTime().ToString("o"),
                    endTime = s.EndTime?.ToLocalTime().ToString("o"),
                    startPct = s.StartPercentage,
                    endPct = s.EndPercentage,
                    durationMin = Math.Round(s.DurationMinutes, 1),
                    dropPct = s.DropPercentage,
                    ratePerHour = Math.Round(s.RatePerHour, 1)
                }),
                hourlyDischarge = hourlyDischarge.Select(h => new
                {
                    hour = h.HourOfDay,
                    avgLevel = Math.Round(h.AverageLevel, 1),
                    count = h.ReadingCount
                }),
                chargeDischargeSummary = chargeDischargeSummary.Select(d => new
                {
                    date = d.Date.ToString("yyyy-MM-dd"),
                    chargeTimeMin = Math.Round(d.ChargeTimeMinutes, 1),
                    dischargeTimeMin = Math.Round(d.DischargeTimeMinutes, 1),
                    avgChargeLevel = Math.Round(d.AverageChargeLevel, 1),
                    avgDischargeLevel = Math.Round(d.AverageDischargeLevel, 1),
                    minLevel = d.MinLevel,
                    maxLevel = d.MaxLevel
                }),
                statistics = new
                {
                    totalSessions = stats.TotalChargingSessions,
                    avgDuration = Math.Round(stats.AverageChargeDurationMinutes, 1),
                    overchargeCount = stats.OverchargeCount,
                    avgOverchargeDuration = Math.Round(stats.AverageOverchargeDurationMinutes, 1)
                }
            };

            var json = JsonSerializer.Serialize(payload);
            _webView.CoreWebView2.PostWebMessageAsJson(json);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send data to WebView", ex);
        }
    }

    private void OnRefreshClick(object? sender, EventArgs e)
    {
        _startDate = _startDatePicker.Value.Date;
        _endDate = _endDatePicker.Value.Date.AddDays(1).AddTicks(-1); // End of day
        SendDataToWebView();
    }
}
