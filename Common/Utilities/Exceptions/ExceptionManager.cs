using Common.IO.Collections;
using Common.Logging;
using Common.Logging.Console;
using Common.Logging.File;
using Common.Reflection;

using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Common.Utilities.Exceptions
{
    public static class ExceptionManager
    {
        private static LockedList<Exception> allExceptions = new LockedList<Exception>();
        private static LockedList<Exception> unhandledExceptions = new LockedList<Exception>();

        public static ExceptionSettings Settings { get; set; } = ExceptionSettings.LogUnhandled | ExceptionSettings.LogHandled;

        public static LogOutput Output { get; } = new LogOutput("Exception Manager");

        public static Func<Exception, string> ExceptionFormatter { get; set; } = ExceptionUtils.FormatException;
        public static Func<StackFrame[], string> TraceFormatter { get; set; } = ExceptionUtils.FormatTrace;

        public static event Action<Exception> OnThrown;
        public static event Action<Exception> OnUnhandled;

        static ExceptionManager()
        {
            allExceptions = new LockedList<Exception>();
            unhandledExceptions = new LockedList<Exception>();

            Settings = ExceptionSettings.LogUnhandled;

            ExceptionFormatter = ExceptionUtils.FormatException;
            TraceFormatter = ExceptionUtils.FormatTrace;

            Output = new LogOutput("Exception Manager");

            Output.AddConsoleIfPresent();
            Output.AddFileWithPrefix("EXCEPTIONS");

            Output.Info($"Initialized!");

            AppDomain.CurrentDomain.FirstChanceException += OnGeneralThrown;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledThrown;
        }

        private static void OnGeneralThrown(object _, FirstChanceExceptionEventArgs ev)
        {
            allExceptions.Add(ev.Exception);
            OnThrown.Call(ev.Exception);

            if ((Settings & ExceptionSettings.LogHandled) != 0)
            {
                var formatted = ExceptionFormatter.Call(ev.Exception);

                if (formatted != null)
                    Output.Error(
                        $"An exception has been intercepted." +
                        $"\n{formatted}");
            }
        }

        private static void OnUnhandledThrown(object _, UnhandledExceptionEventArgs ev)
        {
            if (ev.ExceptionObject is null || ev.ExceptionObject is not Exception exception)
                return;

            unhandledExceptions.Add(exception);
            OnUnhandled.Call(exception);

            if ((Settings & ExceptionSettings.LogUnhandled) != 0)
            {
                var formattedExc = ExceptionFormatter.Call(exception);
                var formattedTrace = TraceFormatter.Call(new StackTrace().GetFrames());

                if (formattedExc != null && formattedTrace != null)
                    Output.Fatal(
                        $"An unhandled exception has been intercepted!" +
                        $"\nTrace:" +
                        $"\n{formattedTrace}" +
                        $"\nException:" +
                        $"\n{exception}");
            }
        }
    }
}