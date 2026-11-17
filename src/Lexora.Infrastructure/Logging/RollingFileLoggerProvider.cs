using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Lexora.Infrastructure.Logging;

/// <summary>
/// Lightweight, thread-safe daily rolling file logger provider for offline desktop diagnostics.
/// Automatically prunes log files older than <see cref="RetentionDays"/>.
/// </summary>
public sealed class RollingFileLoggerProvider : ILoggerProvider
{
    private const int RetentionDays = 14;
    private readonly string _logDirectory;
    private readonly LogLevel _minLevel;
    private readonly object _writeLock = new();
    private readonly ConcurrentDictionary<string, RollingFileLogger> _loggers = new(StringComparer.Ordinal);
    private bool _disposed;

    public RollingFileLoggerProvider(string? logDirectory = null, LogLevel minLevel = LogLevel.Information)
    {
        _logDirectory = string.IsNullOrWhiteSpace(logDirectory)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lexora",
                "logs")
            : logDirectory;
        _minLevel = minLevel;

        try
        {
            Directory.CreateDirectory(_logDirectory);
            CleanupOldLogs();
        }
        catch
        {
            // Logging initialization must never crash application startup
        }
    }

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name => new RollingFileLogger(name, _minLevel, WriteLine));

    private void WriteLine(string entry)
    {
        if (_disposed) return;

        try
        {
            var filePath = Path.Combine(_logDirectory, $"lexora-{DateTime.Now:yyyy-MM-dd}.log");
            lock (_writeLock)
            {
                File.AppendAllText(filePath, entry + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Never throw from logger sinks
        }
    }

    private void CleanupOldLogs()
    {
        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
        foreach (var file in Directory.EnumerateFiles(_logDirectory, "lexora-*.log"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore locked or inaccessible log files
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _loggers.Clear();
    }

    private sealed class RollingFileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LogLevel _minLevel;
        private readonly Action<string> _writeLine;

        public RollingFileLogger(string categoryName, LogLevel minLevel, Action<string> writeLine)
        {
            _categoryName = categoryName;
            _minLevel = minLevel;
            _writeLine = writeLine;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel)
            => logLevel != LogLevel.None && logLevel >= _minLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            var sb = new StringBuilder();
            sb.Append('[')
              .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
              .Append("] [")
              .Append(logLevel.ToString())
              .Append("] [")
              .Append(_categoryName)
              .Append("] ")
              .Append(message);

            if (exception != null)
            {
                sb.AppendLine()
                  .Append("  Exception: ")
                  .Append(exception);
            }

            _writeLine(sb.ToString());
        }
    }
}
