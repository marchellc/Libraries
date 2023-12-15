using Common.Logging.Console;
using Common.Logging.File;
using Common.Utilities;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Common.Logging
{
    public class LogOutput : Disposable
    {
        private string source;
        private List<ILogger> loggers;

        public static LogOutput Common { get; private set; } 

        public LogOutput(string source = null)
        {
            this.source = source;
            this.loggers = new List<ILogger>();

            if (string.IsNullOrWhiteSpace(this.source))
            {
                var method = new StackFrame(0).GetMethod();

                if (method is null || method.DeclaringType is null)
                    this.source = Assembly.GetCallingAssembly().GetName().Name;
                else
                    this.source = $"{method.DeclaringType.Name}";
            }
        }

        public LogLevel Enabled { get; set; } = LogLevel.Information | LogLevel.Warning | LogLevel.Error | LogLevel.Fatal;

        public bool HasLogger<TLogger>() where TLogger : ILogger
            => loggers.Any(t => t is TLogger);

        public ILogger[] GetLoggers()
            => loggers.ToArray();

        public TLogger GetLogger<TLogger>() where TLogger : ILogger
        {
            for (int i = 0; i < loggers.Count; i++)
            {
                if (loggers[i] is TLogger logger)
                    return logger;
            }

            return default;
        }

        public TLogger AddLogger<TLogger>() where TLogger : ILogger, new()
        {
            if (HasLogger<TLogger>())
                return GetLogger<TLogger>();

            var logger = new TLogger();

            loggers.Add(logger);

            return logger;
        }

        public TLogger AddLogger<TLogger>(TLogger logger) where TLogger : ILogger
        {
            if (HasLogger<TLogger>())
                return GetLogger<TLogger>();

            loggers.Add(logger);
            return logger;
        }

        public void Raw(object message, ConsoleColor color = ConsoleColor.White)
        {
            if (message is null || !LogUtils.IsConsoleAvailable)
                return;

            System.Console.ForegroundColor = color;
            System.Console.WriteLine(message);
            System.Console.ResetColor();
        }

        public void Trace(object message)
            => Emit(LogUtils.CreateMessage(source, message.ToString(), LogLevel.Trace));

        public void Debug(object message)
            => Emit(LogUtils.CreateMessage(source, message.ToString(), LogLevel.Debug));

        public void Verbose(object message)
            => Emit(LogUtils.CreateMessage(source, message.ToString(), LogLevel.Verbose));

        public void Info(object message)
            => Emit(LogUtils.CreateMessage(source, message.ToString(), LogLevel.Information));

        public void Warn(object message)
            => Emit(LogUtils.CreateMessage(source, message.ToString(), LogLevel.Warning));

        public void Error(object message)
            => Emit(LogUtils.CreateMessage(source, message.ToString(), LogLevel.Error));

        public void Fatal(object message)
            => Emit(LogUtils.CreateMessage(source, message.ToString(), LogLevel.Fatal));

        public void Emit(LogMessage message)
        {
            message.Output = this;

            if ((message.Level & Enabled) != 0)
            {
                foreach (var logger in loggers)
                {
                    if (logger is null)
                        continue;

                    try
                    {
                        logger.Emit(message);
                    }
                    catch (Exception ex)
                    {
                        Raw($"Failed to display message on logger '{logger.GetType().FullName}':\n{ex}", ConsoleColor.Red);
                    }
                }

                LogEvents.Invoke(message);
            }
        }

        public override void OnDispose()
        {
            foreach (var log in loggers)
            {
                if (log is not Disposable disposable)
                    continue;

                disposable.Dispose();
            }

            loggers.Clear();
            loggers = null;

            source = null;

            if (Common == this)
                Common = null;
        }

        internal static void Init()
        {
            Common = new LogOutput("Common Library");
            Common.AddConsoleIfPresent();
            Common.AddFileWithPrefix("General Log");
        }
    }
}