﻿using Common.Extensions;
using Common.IO.Collections;
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
        private static readonly LockedList<LogOutput> allOutputs = new LockedList<LogOutput>();

        private string source = "";
        private List<ILogger> loggers = new List<ILogger>();

        public static LogOutput Common { get; private set; } 

        public static LogOutput[] Outputs
        {
            get => allOutputs.ToArray();
        }

        public LogOutput(string source = null)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                var method = new StackFrame(0).GetMethod();

                if (method is null || method.DeclaringType is null)
                    Name = Assembly.GetCallingAssembly().GetName().Name;
                else
                    Name = $"{method.DeclaringType.Name}";
            }
            else
                Name = source;

            Enabled = LogUtils.General | LogUtils.Debug;

            allOutputs.Add(this);
        }

        public LogLevel Enabled { get; set; }

        public string Name
        {
            get => source ?? string.Empty;
            set => source = value ?? Assembly.GetCallingAssembly().GetName().Name;
        }

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
                if (log is Disposable apiDisposable)
                    apiDisposable.Dispose();
                else if (log is IDisposable disposable)
                    disposable.Dispose();
            }

            loggers.Clear();
            loggers = null;

            source = null;

            if (Common != null && Common == this)
                Common = null;

            allOutputs.Remove(this);
        }

        public static void Raw(object message, ConsoleColor color = ConsoleColor.White)
        {
            if (message is null || !LogUtils.IsConsoleAvailable)
                return;

            System.Console.ForegroundColor = color;
            System.Console.WriteLine(message);
            System.Console.ResetColor();
        }

        public static void AddToAll<TLogger>() where TLogger : ILogger, new()
            => AddToAll(typeof(TLogger).Construct() as ILogger);

        public static void AddToAll(ILogger logger)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));

            foreach (var output in allOutputs)
            {
                if (!output.loggers.Any(l => l.GetType() == logger.GetType()))
                    output.AddLogger(logger);
            }
        }

        public static void RemoveFromAll<TLogger>() where TLogger : ILogger, new()
            => RemoveFromAll(typeof(TLogger));

        public static void RemoveFromAll(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (!typeof(ILogger).IsAssignableFrom(type))
                throw new Exception($"Type '{type.FullName}' is not a subclass of ILogger");

            foreach (var output in allOutputs)
                output.loggers?.RemoveAll(l => l.GetType() == type);
        }

        internal static void Init()
        {
            Common = new LogOutput("Common Library");

            Common.AddConsoleIfPresent();
            Common.AddFileWithPrefix("General Log");

            Common.Info("Logging initialized.");
        }
    }
}