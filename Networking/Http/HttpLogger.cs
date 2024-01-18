using Common.Extensions;
using Common.Logging;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Networking.Http
{
    public class HttpLogger : ILoggerProvider, ILogger, IDisposable, ILoggerFactory
    {
        public List<LogLevel> EnabledLevels { get; }
        public LogOutput Output { get; set; }

        public event Action<LogLevel, Exception, string> OnLog;

        public HttpLogger()
        {
            EnabledLevels = new List<LogLevel>(Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>());
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull 
            => this;

        public ILogger CreateLogger(string categoryName)
            => new HttpLogger();

        public void Dispose() { }
        public void AddProvider(ILoggerProvider provider) { }

        public bool IsEnabled(LogLevel logLevel)
            => EnabledLevels.Contains(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var msg = formatter.Call(state, exception);

            if (string.IsNullOrWhiteSpace(msg))
                return;

            OnLog.Call(logLevel, exception, msg);

            switch (logLevel)
            {
                case LogLevel.Trace:
                    Output?.Trace(msg);
                    break;

                case LogLevel.Warning:
                    Output?.Warn(msg);
                    break;

                case LogLevel.Error:
                    Output?.Error(msg);
                    break;

                case LogLevel.Information:
                    Output?.Info(msg);
                    break;

                case LogLevel.Debug:
                    Output?.Debug(msg);
                    break;

                case LogLevel.Critical:
                    Output?.Fatal(msg);
                    break;
            }
        }
    }
}
