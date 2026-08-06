using Microsoft.Win32;
using ChargeGuard.Logging;

namespace ChargeGuard.Settings;

/// <summary>
/// Manages Windows startup registration for the current user.
/// </summary>
public class StartupManager
{
    private readonly string _executablePath;
    private readonly IAppLogger _logger;
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "ChargeGuard";

    public StartupManager(string executablePath, IAppLogger logger)
    {
        _executablePath = executablePath;
        _logger = logger;
    }

    /// <summary>
    /// Checks whether the application is registered to start with Windows.
    /// </summary>
    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            if (key == null)
            {
                return false;
            }

            var currentValue = key.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(currentValue) && 
                   currentValue.Equals(GetQuotedExecutablePath(), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to check startup status", ex);
            return false;
        }
    }

    /// <summary>
    /// Enables or disables startup with Windows.
    /// </summary>
    public bool SetStartupEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null)
            {
                _logger.LogError("Failed to open Run registry key for writing");
                return false;
            }

            if (enabled)
            {
                var quotedPath = GetQuotedExecutablePath();
                key.SetValue(AppName, quotedPath);
                _logger.LogInfo($"Startup registration enabled: {quotedPath}");
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
                _logger.LogInfo("Startup registration disabled");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to modify startup registration", ex);
            return false;
        }
    }

    private string GetQuotedExecutablePath()
    {
        // Properly quote the executable path to handle spaces
        if (_executablePath.Contains(' '))
        {
            return $"\"{_executablePath}\"";
        }
        return _executablePath;
    }

    /// <summary>
    /// Gets the executable path being used for startup registration.
    /// </summary>
    public string GetExecutablePath()
    {
        return _executablePath;
    }
}
