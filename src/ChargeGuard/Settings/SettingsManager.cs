using System.Text.Json;
using ChargeGuard.Logging;

namespace ChargeGuard.Settings;

/// <summary>
/// Manages loading and saving application settings.
/// </summary>
public class SettingsManager
{
    private readonly string _settingsFilePath;
    private readonly IAppLogger _logger;
    private readonly object _lock = new();
    private ChargeGuardSettings? _cachedSettings;

    private const int CurrentSettingsVersion = 1;

    public SettingsManager(string settingsDirectory, IAppLogger logger)
    {
        _logger = logger;

        if (!Directory.Exists(settingsDirectory))
        {
            try
            {
                Directory.CreateDirectory(settingsDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to create settings directory", ex);
                // Fallback to temp directory
                settingsDirectory = Path.Combine(Path.GetTempPath(), "ChargeGuard");
                if (!Directory.Exists(settingsDirectory))
                {
                    Directory.CreateDirectory(settingsDirectory);
                }
            }
        }

        _settingsFilePath = Path.Combine(settingsDirectory, "settings.json");
    }

    /// <summary>
    /// Loads the settings from file, or creates default settings if the file doesn't exist.
    /// </summary>
    public ChargeGuardSettings LoadSettings()
    {
        lock (_lock)
        {
            if (_cachedSettings != null)
            {
                return _cachedSettings.Clone();
            }

            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogInfo("Settings file not found, creating default settings");
                var defaultSettings = CreateDefaultSettings();
                SaveSettings(defaultSettings);
                _cachedSettings = defaultSettings;
                return defaultSettings.Clone();
            }

            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<ChargeGuardSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (settings == null)
                {
                    _logger.LogWarning("Deserialized settings were null, using defaults");
                    var defaultSettings = CreateDefaultSettings();
                    SaveSettings(defaultSettings);
                    _cachedSettings = defaultSettings;
                    return defaultSettings.Clone();
                }

                settings.ValidateAndNormalize();
                _cachedSettings = settings;
                _logger.LogInfo("Settings loaded successfully");
                return settings.Clone();
            }
            catch (JsonException ex)
            {
                _logger.LogError("Failed to parse settings file, using defaults", ex);

                // Preserve corrupt file for diagnostics
                var corruptBackupPath = _settingsFilePath + ".corrupt";
                try
                {
                    File.Copy(_settingsFilePath, corruptBackupPath, overwrite: true);
                    _logger.LogInfo($"Corrupt settings backed up to: {corruptBackupPath}");
                }
                catch
                {
                    // Ignore backup failures
                }

                var defaultSettings = CreateDefaultSettings();
                SaveSettings(defaultSettings);
                _cachedSettings = defaultSettings;
                return defaultSettings.Clone();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load settings, using defaults", ex);
                var defaultSettings = CreateDefaultSettings();
                _cachedSettings = defaultSettings;
                return defaultSettings.Clone();
            }
        }
    }

    /// <summary>
    /// Saves the settings to file atomically.
    /// </summary>
    public void SaveSettings(ChargeGuardSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        lock (_lock)
        {
            settings.ValidateAndNormalize();

            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Atomic write: write to temp file, then move
                var tempFilePath = _settingsFilePath + ".tmp";
                File.WriteAllText(tempFilePath, json);

                // Replace original file
                File.Move(tempFilePath, _settingsFilePath, overwrite: true);

                _cachedSettings = settings.Clone();
                _logger.LogInfo("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save settings", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Gets the settings file path for diagnostic purposes.
    /// </summary>
    public string GetSettingsFilePath()
    {
        return _settingsFilePath;
    }

    private static ChargeGuardSettings CreateDefaultSettings()
    {
        return new ChargeGuardSettings();
    }
}
