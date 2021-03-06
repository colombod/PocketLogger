using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;

namespace Pocket.For.MicrosoftExtensionsLogging
{
    internal static class LoggerFactoryExtensions
    {
        public static ILoggerFactory AddPocketLogger(
            this ILoggerFactory factory,
            Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter =null)
        {
            factory.AddProvider(
                new LoggerProvider(category => new Logger(category, filter)));

            return factory;
        }

        private class Logger : ILogger
        {
            private readonly string category;

            private readonly Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter;

            public Logger(
                string category,
                Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter = null)
            {
                this.category = category;
                this.filter = filter;
            }

            public void Log<TState>(
                Microsoft.Extensions.Logging.LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var logEntry = new LogEntry(
                    logLevel: logLevel.ToPocketLoggerLogLevel(),
                    message: state.ToString(),
                    exception: exception,
                    category: category);

                if (state is FormattedLogValues formattedLogValues)
                {
                    foreach (var value in formattedLogValues)
                    {
                        if (value.Key != "{OriginalFormat}")
                        {
                            logEntry.AddProperty((value.Key, value.Value));
                        }
                    }
                }

                Pocket.Logger.Log.Post(logEntry);
            }

            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) =>
                filter == null || filter(category, logLevel);

            public IDisposable BeginScope<TState>(TState state) =>
                new OperationLogger(
                    operationName: state.ToLogString(),
                    category: category,
                    logOnStart: true);
        }

        private class LoggerProvider : ILoggerProvider
        {
            private readonly Func<string, ILogger> createLogger;

            public LoggerProvider(
                Func<string, ILogger> createLogger) =>
                this.createLogger =
                    createLogger ?? throw new ArgumentNullException(nameof(createLogger));

            public ILogger CreateLogger(string categoryName) => createLogger(categoryName);

            public void Dispose()
            {
            }
        }

        private static LogLevel ToPocketLoggerLogLevel(this Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            switch (logLevel)
            {
                case Microsoft.Extensions.Logging.LogLevel.Trace:
                    return LogLevel.Trace;

                case Microsoft.Extensions.Logging.LogLevel.Debug:
                    return LogLevel.Debug;

                case Microsoft.Extensions.Logging.LogLevel.Information:
                    return LogLevel.Information;

                case Microsoft.Extensions.Logging.LogLevel.Warning:
                    return LogLevel.Warning;

                case Microsoft.Extensions.Logging.LogLevel.Error:
                    return LogLevel.Error;

                case Microsoft.Extensions.Logging.LogLevel.Critical:
                    return LogLevel.Critical;

                default:
                    return LogLevel.Information;
            }
        }
    }
}
