using Common.Reflection;

using System;

namespace Common.Logging
{
    public static class LogEvents
    {
        public static event Action<LogMessage> OnWritten;

        internal static void Invoke(LogMessage message) { OnWritten.Call(message); }
    }
}