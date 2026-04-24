using System.Diagnostics;
using System.Globalization;
using System.Text;

using Microsoft.Extensions.Logging;

namespace TibiaHuntMaster.App.Services.Diagnostics
{
    public sealed class RollingFileLoggerProvider : ILoggerProvider
    {
        private const int MaxRetainedLogFiles = 10;

        private readonly object _syncRoot = new();
        private readonly AppDataPaths _paths;
        private readonly LogLevel _minimumLevel;
        private bool _disposed;

        public RollingFileLoggerProvider(AppDataPaths paths)
        {
            _paths = paths;
            _minimumLevel = Debugger.IsAttached ? LogLevel.Debug : LogLevel.Information;
            _paths.EnsureDirectories();
            TrimOldLogFiles();
        }

        public ILogger CreateLogger(string categoryName)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return new RollingFileLogger(this, categoryName);
        }

        public void Dispose()
        {
            _disposed = true;
        }

        internal bool IsEnabled(LogLevel logLevel)
        {
            return !_disposed && logLevel != LogLevel.None && logLevel >= _minimumLevel;
        }

        internal void WriteLog(string categoryName, LogLevel logLevel, EventId eventId, string message, Exception? exception)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            lock (_syncRoot)
            {
                _paths.EnsureDirectories();

                string filePath = Path.Combine(_paths.LogsDirectory, $"app-{DateTime.UtcNow:yyyyMMdd}.log");
                TrimOldLogFiles();

                StringBuilder builder = new();
                builder.Append(DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture));
                builder.Append(' ');
                builder.Append('[');
                builder.Append(logLevel);
                builder.Append(']');
                builder.Append(' ');
                builder.Append(categoryName);

                if (eventId.Id != 0 || !string.IsNullOrWhiteSpace(eventId.Name))
                {
                    builder.Append(" (Event ");
                    builder.Append(eventId.Id.ToString(CultureInfo.InvariantCulture));
                    if (!string.IsNullOrWhiteSpace(eventId.Name))
                    {
                        builder.Append(':');
                        builder.Append(eventId.Name);
                    }

                    builder.Append(')');
                }

                builder.Append(": ");
                builder.AppendLine(message);

                if (exception != null)
                {
                    builder.AppendLine(exception.ToString());
                }

                File.AppendAllText(filePath, builder.ToString(), Encoding.UTF8);
            }
        }

        private void TrimOldLogFiles()
        {
            IEnumerable<string> staleFiles = Directory.EnumerateFiles(_paths.LogsDirectory, "app-*.log")
                                                    .OrderByDescending(File.GetLastWriteTimeUtc)
                                                    .Skip(MaxRetainedLogFiles);

            foreach (string staleFile in staleFiles)
            {
                try
                {
                    File.Delete(staleFile);
                }
                catch
                {
                    // Diagnostics logging must not crash the app if cleanup fails.
                }
            }
        }

        private sealed class RollingFileLogger : ILogger
        {
            private readonly RollingFileLoggerProvider _provider;
            private readonly string _categoryName;

            public RollingFileLogger(RollingFileLoggerProvider provider, string categoryName)
            {
                _provider = provider;
                _categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return _provider.IsEnabled(logLevel);
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!_provider.IsEnabled(logLevel))
                {
                    return;
                }

                string message = formatter(state, exception);
                if (string.IsNullOrWhiteSpace(message) && exception is null)
                {
                    return;
                }

                _provider.WriteLog(_categoryName, logLevel, eventId, message, exception);
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
