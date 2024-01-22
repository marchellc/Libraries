using Common.Logging;
using Common.Logging.Console;
using Common.Logging.File;
using Common.Extensions;
using Common.IO.Collections;
using Common.Attributes.Custom;

using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Common.Utilities.Exceptions
{
    public static class ExceptionManager
    {
        private static LockedList<Exception> allExceptions;
        private static LockedList<Exception> unhandledExceptions;

        public static ExceptionSettings Settings { get; set; }

        public static LogOutput Output { get; private set; } 

        public static Func<Exception, string> ExceptionFormatter { get; set; } 
        public static Func<StackFrame[], string> TraceFormatter { get; set; }

        public static LockedList<Exception> AllExceptions => allExceptions;
        public static LockedList<Exception> UnhandledExceptions => unhandledExceptions;

        [Init]
        private static void Init()
        {
            allExceptions = new LockedList<Exception>();
            unhandledExceptions = new LockedList<Exception>();

            Settings = ExceptionSettings.LogUnhandled | ExceptionSettings.LogHandled;

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