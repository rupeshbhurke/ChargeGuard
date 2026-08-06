using System.Text;

namespace ChargeGuard.Logging;

/// <summary>
/// Rolling file logger that writes to text files with automatic rotation.
/// </summary>
public class RollingFileLogger : IAppLogger, IDisposable
{
    private readonly string _logDirectory;
    private readonly string _logFilePrefix;
    private readonly long _maxFileSizeBytes;
    private readonly int _maxRetainedFiles;
    private readonly object _lock = new();
    private StreamWriter? _currentWriter;
    private string? _currentLogFilePath;
    private bool _disposed;

    private const long DefaultMaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const int DefaultMaxRetainedFiles = 10;

    public RollingFileLogger(
        string logDirectory,
        string logFilePrefix = "ChargeGuard",
        long maxFileSizeBytes = DefaultMaxFileSizeBytes,
        int maxRetainedFiles = DefaultMaxRetainedFiles)
    {
        _logDirectory = logDirectory;
        _logFilePrefix = logFilePrefix;
        _maxFileSizeBytes = maxFileSizeBytes;
        _maxRetainedFiles = maxRetainedFiles;

        InitializeLogDirectory();
        InitializeCurrentLogFile();
    }

    private void InitializeLogDirectory()
    {
        try
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }
        catch
        {
            // Fallback to temp directory if user directory fails
            var tempDir = Path.Combine(Path.GetTempPath(), "ChargeGuard", "Logs");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
        }
    }

    private void InitializeCurrentLogFile()
    {
        lock (_lock)
        {
            CleanupOldLogFiles();
            RotateIfNeeded();
            EnsureCurrentWriter();
        }
    }

    private void CleanupOldLogFiles()
    {
        try
        {
            if (!Directory.Exists(_logDirectory))
                return;

            var logFiles = Directory.GetFiles(_logDirectory, $"{_logFilePrefix}_*.log")
                .OrderByDescending(f => f)
                .ToList();

            // Remove files beyond the retention limit
            while (logFiles.Count > _maxRetainedFiles)
            {
                var oldFile = logFiles.Last();
                try
                {
                    File.Delete(oldFile);
                }
                catch
                {
                    // Ignore deletion failures
                }
                logFiles.RemoveAt(logFiles.Count - 1);
            }
        }
        catch
        {
            // Ignore cleanup failures
        }
    }

    private void RotateIfNeeded()
    {
        try
        {
            if (!Directory.Exists(_logDirectory))
                return;

            var logFiles = Directory.GetFiles(_logDirectory, $"{_logFilePrefix}_*.log")
                .OrderByDescending(f => f)
                .ToList();

            if (logFiles.Count > 0)
            {
                var latestFile = logFiles.First();
                var fileInfo = new FileInfo(latestFile);

                if (fileInfo.Exists && fileInfo.Length >= _maxFileSizeBytes)
                {
                    // Current file is too large, create a new one
                    _currentLogFilePath = null;
                }
                else
                {
                    _currentLogFilePath = latestFile;
                }
            }
        }
        catch
        {
            _currentLogFilePath = null;
        }
    }

    private void EnsureCurrentWriter()
    {
        if (_currentWriter != null && _currentLogFilePath != null)
        {
            // Check if current file still exists and is writable
            try
            {
                var fileInfo = new FileInfo(_currentLogFilePath);
                if (fileInfo.Exists && fileInfo.Length < _maxFileSizeBytes)
                {
                    return; // Current writer is still valid
                }
            }
            catch
            {
                // File check failed, need new writer
            }

            // Close current writer
            try
            {
                _currentWriter.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
            _currentWriter = null;
        }

        // Create new log file
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _currentLogFilePath = Path.Combine(_logDirectory, $"{_logFilePrefix}_{timestamp}.log");

        try
        {
            _currentWriter = new StreamWriter(_currentLogFilePath, true, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }
        catch
        {
            _currentWriter = null;
            _currentLogFilePath = null;
        }
    }

    public void LogInfo(string message)
    {
        WriteLog("INFO", message);
    }

    public void LogWarning(string message)
    {
        WriteLog("WARN", message);
    }

    public void LogError(string message, Exception? exception = null)
    {
        var fullMessage = message;
        if (exception != null)
        {
            fullMessage += $" | {exception.GetType().Name}: {exception.Message}";
            if (exception.StackTrace != null)
            {
                fullMessage += Environment.NewLine + exception.StackTrace;
            }
        }
        WriteLog("ERROR", fullMessage);
    }

    public void LogDebug(string message)
    {
        WriteLog("DEBUG", message);
    }

    private void WriteLog(string level, string message)
    {
        if (_disposed)
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logLine = $"[{timestamp}] [{level}] {message}";

        lock (_lock)
        {
            try
            {
                EnsureCurrentWriter();
                _currentWriter?.WriteLine(logLine);
            }
            catch
            {
                // Silently ignore logging failures
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            try
            {
                _currentWriter?.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
            _currentWriter = null;
            _disposed = true;
        }
    }
}
