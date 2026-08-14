using System.Windows.Forms;
using ChargeGuard.Application;
using ChargeGuard.Battery;
using ChargeGuard.Charging;
using ChargeGuard.Logging;
using ChargeGuard.Notifications;
using ChargeGuard.Settings;
using WinFormsApplication = System.Windows.Forms.Application;

namespace ChargeGuard;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        WinFormsApplication.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        WinFormsApplication.EnableVisualStyles();
        WinFormsApplication.SetCompatibleTextRenderingDefault(false);

        // Install unhandled exception handler
        WinFormsApplication.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        WinFormsApplication.ThreadException += OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        RollingFileLogger? logger = null;
        SingleInstanceManager? singleInstanceManager = null;
        ChargeGuardApplicationContext? applicationContext = null;

        try
        {
            // Initialize logging
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ChargeGuard",
                "Logs");

            logger = new RollingFileLogger(logDirectory);
            logger.LogInfo("ChargeGuard starting");
            logger.LogInfo($"Version: {GetApplicationVersion()}");

            // Check single instance
            singleInstanceManager = new SingleInstanceManager(logger);
            if (!singleInstanceManager.TryAcquireMutex())
            {
                logger.LogInfo("Another instance is already running, exiting");
                MessageBox.Show(
                    "ChargeGuard is already running.",
                    "ChargeGuard",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Load settings
            var settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ChargeGuard");

            var settingsManager = new SettingsManager(settingsDirectory, logger);
            var settings = settingsManager.LoadSettings();
            logger.LogInfo($"Settings loaded from: {settingsManager.GetSettingsFilePath()}");

            // Initialize startup manager
            var executablePath = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine executable path");
            var startupManager = new StartupManager(executablePath, logger);

            // Sync startup setting with registry
            if (settings.StartWithWindows && !startupManager.IsStartupEnabled())
            {
                startupManager.SetStartupEnabled(true);
            }
            else if (!settings.StartWithWindows && startupManager.IsStartupEnabled())
            {
                startupManager.SetStartupEnabled(false);
            }

            // Initialize battery monitor
            var batteryMonitor = new BatteryMonitor(logger);

            // Initialize alert evaluator
            var alertEvaluator = new ChargingAlertEvaluator(settings);

            // Initialize sound player
            var soundPlayer = new SystemSoundPlayer();

            // Create application context
            applicationContext = new ChargeGuardApplicationContext(
                logger,
                settings,
                settingsManager,
                startupManager,
                batteryMonitor,
                alertEvaluator,
                soundPlayer);

            // Start the application
            applicationContext.Start();

            // Run the application
            WinFormsApplication.Run(applicationContext);

            // Cleanup on exit
            try
            {
                applicationContext.Stop();
                singleInstanceManager.Dispose();
                logger.Dispose();
            }
            catch (Exception ex)
            {
                logger?.LogError("Error during shutdown", ex);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError("Failed to start ChargeGuard", ex);
            MessageBox.Show(
                $"Failed to start ChargeGuard: {ex.Message}",
                "ChargeGuard Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void OnThreadException(object? sender, System.Threading.ThreadExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unhandled exception occurred: {e.Exception?.Message}",
            "ChargeGuard Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show(
                $"An unhandled exception occurred: {ex.Message}",
                "ChargeGuard Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static string GetApplicationVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString(3) ?? "1.0.0";
    }
}
