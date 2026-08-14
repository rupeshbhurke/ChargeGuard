(function () {
    'use strict';

    const PLOT_CONFIG = {
        responsive: true,
        displaylogo: false,
        modeBarButtonsToRemove: ['lasso2d', 'select2d'],
        scrollZoom: true,
    };

    const PLOT_LAYOUT_BASE = {
        paper_bgcolor: 'rgba(0,0,0,0)',
        plot_bgcolor: 'rgba(15,52,96,0.3)',
        font: { color: '#e0e0e0', family: 'Segoe UI, sans-serif', size: 12 },
        margin: { t: 30, r: 20, b: 50, l: 60 },
        legend: { bgcolor: 'rgba(0,0,0,0)', orientation: 'h', y: -0.2 },
        xaxis: { gridcolor: '#2a2a4a', zerolinecolor: '#2a2a4a' },
        yaxis: { gridcolor: '#2a2a4a', zerolinecolor: '#2a2a4a' },
    };

    function deepClone(obj) {
        return JSON.parse(JSON.stringify(obj));
    }

    function formatTime(ts) {
        const d = new Date(ts);
        return d.toLocaleString('en-US', {
            month: 'short', day: 'numeric',
            hour: '2-digit', minute: '2-digit'
        });
    }

    function formatDate(ts) {
        const d = new Date(ts);
        return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    }

    // === Chart 1: Battery Level Over Time ===
    function renderBatteryLevelChart(data) {
        if (!data.batteryReadings || data.batteryReadings.length === 0) {
            Plotly.purge('plot-battery-level');
            document.getElementById('plot-battery-level').innerHTML =
                '<p style="text-align:center;color:#a0a0a0;padding:40px;">No battery readings available</p>';
            return;
        }

        const readings = data.batteryReadings;
        const timestamps = readings.map(r => r.timestamp);
        const percentages = readings.map(r => r.percentage);

        // Create color-coded segments: blue for charging, red for discharging
        const chargingTrace = {
            x: [], y: [], mode: 'lines+markers', name: 'Charging',
            line: { color: '#0078d7', width: 2 },
            marker: { size: 4, color: '#0078d7' },
            hovertemplate: '<b>%{text}</b><br>Level: %{y}%<br>Charging<extra></extra>',
            text: [],
        };

        const dischargingTrace = {
            x: [], y: [], mode: 'lines+markers', name: 'Discharging',
            line: { color: '#dc3545', width: 2 },
            marker: { size: 4, color: '#dc3545' },
            hovertemplate: '<b>%{text}</b><br>Level: %{y}%<br>Discharging<extra></extra>',
            text: [],
        };

        readings.forEach(r => {
            const target = r.isCharging ? chargingTrace : dischargingTrace;
            target.x.push(r.timestamp);
            target.y.push(r.percentage);
            target.text.push(formatTime(r.timestamp));
        });

        const layout = deepClone(PLOT_LAYOUT_BASE);
        layout.title = { text: 'Battery %', font: { size: 13 } };
        layout.xaxis.title = 'Time';
        layout.yaxis.title = 'Battery %';
        layout.yaxis.range = [0, 100];
        layout.hovermode = 'closest';

        Plotly.newPlot('plot-battery-level', [chargingTrace, dischargingTrace], layout, PLOT_CONFIG);
    }

    // === Chart 2: Discharge Rate Curves ===
    function renderDischargeRateChart(data) {
        if (!data.dischargeReadings || data.dischargeReadings.length === 0) {
            Plotly.purge('plot-discharge-rate');
            document.getElementById('plot-discharge-rate').innerHTML =
                '<p style="text-align:center;color:#a0a0a0;padding:40px;">No discharge data available</p>';
            return;
        }

        const readings = data.dischargeReadings;
        const x = readings.map(r => r.timestamp);
        const y = readings.map(r => r.percentage);

        // Calculate discharge rate (%/hr) between consecutive readings
        const rateX = [];
        const rateY = [];
        for (let i = 1; i < readings.length; i++) {
            const dt = (new Date(readings[i].timestamp) - new Date(readings[i - 1].timestamp)) / 3600000;
            if (dt > 0) {
                const drop = readings[i - 1].percentage - readings[i].percentage;
                rateX.push(readings[i].timestamp);
                rateY.push(Math.round(drop / dt * 100) / 100);
            }
        }

        const levelTrace = {
            x: x, y: y, mode: 'lines', name: 'Battery %',
            line: { color: '#dc3545', width: 2 },
            yaxis: 'y',
            hovertemplate: '<b>%{x}</b><br>Level: %{y}%<extra></extra>',
        };

        const rateTrace = {
            x: rateX, y: rateY, mode: 'lines', name: 'Discharge Rate (%/hr)',
            line: { color: '#fd7e14', width: 2, dash: 'dot' },
            yaxis: 'y2',
            hovertemplate: '<b>%{x}</b><br>Rate: %{y}%/hr<extra></extra>',
        };

        const layout = deepClone(PLOT_LAYOUT_BASE);
        layout.title = { text: 'Discharge Level & Rate', font: { size: 13 } };
        layout.xaxis.title = 'Time';
        layout.yaxis.title = 'Battery %';
        layout.yaxis.range = [0, 100];
        layout.yaxis2 = {
            title: 'Rate (%/hr)',
            overlaying: 'y',
            side: 'right',
            gridcolor: 'rgba(0,0,0,0)',
            color: '#fd7e14',
        };
        layout.hovermode = 'closest';

        Plotly.newPlot('plot-discharge-rate', [levelTrace, rateTrace], layout, PLOT_CONFIG);
    }

    // === Chart 3: Discharge Session Analysis ===
    function renderDischargeSessionsChart(data) {
        if (!data.dischargeSessions || data.dischargeSessions.length === 0) {
            Plotly.purge('plot-discharge-sessions');
            document.getElementById('plot-discharge-sessions').innerHTML =
                '<p style="text-align:center;color:#a0a0a0;padding:40px;">No discharge sessions detected</p>';
            return;
        }

        const sessions = data.dischargeSessions;
        const x = sessions.map(s => s.startTime);
        const durations = sessions.map(s => s.durationMin);
        const drops = sessions.map(s => s.dropPct);
        const rates = sessions.map(s => s.ratePerHour);

        // Bar chart: duration per session, colored by drop amount
        const durationTrace = {
            x: x, y: durations, type: 'bar', name: 'Duration (min)',
            marker: {
                color: drops,
                colorscale: 'YlOrRd',
                showscale: true,
                colorbar: { title: 'Drop %', thickness: 10 },
            },
            hovertemplate: '<b>%{x}</b><br>Duration: %{y} min<br>Drop: %{customdata}%<extra></extra>',
            customdata: drops,
        };

        const layout = deepClone(PLOT_LAYOUT_BASE);
        layout.title = { text: 'Session Duration (color = drop %)', font: { size: 13 } };
        layout.xaxis.title = 'Session Start';
        layout.yaxis.title = 'Duration (min)';
        layout.hovermode = 'closest';
        layout.bargap = 0.3;

        Plotly.newPlot('plot-discharge-sessions', [durationTrace], layout, PLOT_CONFIG);
    }

    // === Chart 4: Daily Discharge Heatmap ===
    function renderHeatmapChart(data) {
        const el = document.getElementById('plot-heatmap');
        if (!data.hourlyDischarge || data.hourlyDischarge.length === 0) {
            Plotly.purge('plot-heatmap');
            el.innerHTML = '<p style="text-align:center;color:#a0a0a0;padding:40px;">No hourly discharge data available</p>';
            return;
        }

        // Build a matrix: rows = hours (0-23), columns = days in range
        // Since we only have aggregated hourly data (not per-day), we'll show
        // a single-column heatmap by hour for now, plus a bar chart of avg level by hour

        const hours = data.hourlyDischarge.map(h => h.hour + ':00');
        const avgLevels = data.hourlyDischarge.map(h => h.avgLevel);
        const counts = data.hourlyDischarge.map(h => h.count);

        const barTrace = {
            x: hours,
            y: avgLevels,
            type: 'bar',
            name: 'Avg Battery %',
            marker: {
                color: avgLevels,
                colorscale: 'RdYlGn',
                showscale: true,
                colorbar: { title: 'Avg %', thickness: 10 },
            },
            hovertemplate: '<b>Hour %{x}</b><br>Avg Level: %{y}%<br>Readings: %{customdata}<extra></extra>',
            customdata: counts,
        };

        const layout = deepClone(PLOT_LAYOUT_BASE);
        layout.title = { text: 'Avg Battery Level by Hour (Discharging)', font: { size: 13 } };
        layout.xaxis.title = 'Hour of Day';
        layout.yaxis.title = 'Avg Battery %';
        layout.yaxis.range = [0, 100];
        layout.hovermode = 'closest';
        layout.bargap = 0.2;

        Plotly.newPlot('plot-heatmap', [barTrace], layout, PLOT_CONFIG);
    }

    // === Chart 5: Charge vs Discharge Comparison ===
    function renderComparisonChart(data) {
        if (!data.chargeDischargeSummary || data.chargeDischargeSummary.length === 0) {
            Plotly.purge('plot-comparison');
            document.getElementById('plot-comparison').innerHTML =
                '<p style="text-align:center;color:#a0a0a0;padding:40px;">No comparison data available</p>';
            return;
        }

        const summary = data.chargeDischargeSummary;
        const dates = summary.map(d => d.date);
        const chargeTimes = summary.map(d => d.chargeTimeMin);
        const dischargeTimes = summary.map(d => d.dischargeTimeMin);
        const avgChargeLevels = summary.map(d => d.avgChargeLevel);
        const avgDischargeLevels = summary.map(d => d.avgDischargeLevel);

        const chargeTrace = {
            x: dates, y: chargeTimes, type: 'bar', name: 'Charge Time (min)',
            marker: { color: '#28a745' },
            hovertemplate: '<b>%{x}</b><br>Charge: %{y} min<extra></extra>',
        };

        const dischargeTrace = {
            x: dates, y: dischargeTimes, type: 'bar', name: 'Discharge Time (min)',
            marker: { color: '#dc3545' },
            hovertemplate: '<b>%{x}</b><br>Discharge: %{y} min<extra></extra>',
        };

        const avgChargeLine = {
            x: dates, y: avgChargeLevels, type: 'scatter', mode: 'lines+markers',
            name: 'Avg Charge %', line: { color: '#0078d7', width: 2 },
            yaxis: 'y2',
            hovertemplate: '<b>%{x}</b><br>Avg Charge: %{y}%<extra></extra>',
        };

        const avgDischargeLine = {
            x: dates, y: avgDischargeLevels, type: 'scatter', mode: 'lines+markers',
            name: 'Avg Discharge %', line: { color: '#fd7e14', width: 2, dash: 'dot' },
            yaxis: 'y2',
            hovertemplate: '<b>%{x}</b><br>Avg Discharge: %{y}%<extra></extra>',
        };

        const layout = deepClone(PLOT_LAYOUT_BASE);
        layout.title = { text: 'Daily Charge vs Discharge Time & Levels', font: { size: 13 } };
        layout.xaxis.title = 'Date';
        layout.yaxis.title = 'Time (min)';
        layout.yaxis2 = {
            title: 'Battery %',
            overlaying: 'y',
            side: 'right',
            range: [0, 100],
            gridcolor: 'rgba(0,0,0,0)',
            color: '#0078d7',
        };
        layout.barmode = 'group';
        layout.hovermode = 'closest';

        Plotly.newPlot('plot-comparison',
            [chargeTrace, dischargeTrace, avgChargeLine, avgDischargeLine],
            layout, PLOT_CONFIG);
    }

    // === Update stats bar ===
    function updateStats(stats) {
        document.getElementById('stat-sessions').textContent = stats.totalSessions || 0;
        document.getElementById('stat-avg-duration').textContent =
            stats.avgDuration ? stats.avgDuration + ' min' : '--';
        document.getElementById('stat-overcharge').textContent = stats.overchargeCount || 0;
        document.getElementById('stat-avg-overcharge').textContent =
            stats.avgOverchargeDuration ? stats.avgOverchargeDuration + ' min' : '--';
    }

    // === Chart selector handler ===
    function setupChartSelector() {
        const selector = document.getElementById('chart-selector');
        const chartsGrid = document.getElementById('charts-grid');
        const chartContainers = {
            'battery-level': document.getElementById('chart-battery-level'),
            'discharge-rate': document.getElementById('chart-discharge-rate'),
            'discharge-sessions': document.getElementById('chart-discharge-sessions'),
            'heatmap': document.getElementById('chart-heatmap'),
            'comparison': document.getElementById('chart-comparison')
        };

        selector.addEventListener('change', function () {
            const selected = this.value;

            // Remove active class from all charts
            Object.values(chartContainers).forEach(container => {
                if (container) container.classList.remove('active');
            });

            if (selected === 'all') {
                // Show all charts
                chartsGrid.classList.remove('single-chart-mode');
            } else {
                // Show only selected chart
                chartsGrid.classList.add('single-chart-mode');
                if (chartContainers[selected]) {
                    chartContainers[selected].classList.add('active');
                }
            }
        });
    }

    // === Render all charts from data payload ===
    function renderAllCharts(data) {
        updateStats(data.statistics || {});
        renderBatteryLevelChart(data);
        renderDischargeRateChart(data);
        renderDischargeSessionsChart(data);
        renderHeatmapChart(data);
        renderComparisonChart(data);

        // Hide loading overlay
        document.getElementById('loading-overlay').classList.add('hidden');
    }

    // === C# ↔ JS Message Bridge ===
    function requestDataFromHost() {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage('requestData');
        }
    }

    function setupMessageHandler() {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.addEventListener('message', function (event) {
                try {
                    var data = typeof event.data === 'string'
                        ? JSON.parse(event.data)
                        : event.data;
                    renderAllCharts(data);
                } catch (e) {
                    console.error('Error parsing data from host:', e);
                    document.getElementById('loading-overlay').classList.add('hidden');
                    document.getElementById('no-data-message').style.display = 'block';
                }
            });
        }
    }

    // === Init ===
    document.addEventListener('DOMContentLoaded', function () {
        setupMessageHandler();
        setupChartSelector();
        // Request data from C# host shortly after load
        setTimeout(requestDataFromHost, 300);
    });
})();
