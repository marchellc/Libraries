using Common.Attributes.Custom;
using Common.Logging;

using System;

namespace Common.Utilities
{
    public static class Assert
    {
        public static bool IsForced;

        public static bool IsEnabled => IsForced || ModuleInitializer.IsDebugBuild;

        public static readonly LogOutput Log = new LogOutput("Assert");

        [Init]
        internal static void Init()
        {
            if (!IsEnabled)
                return;

            Log.Setup();
        }

        public static void Null(params object[] objs)
        {
            if (!IsEnabled)
                return;

            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i] is null)
                {
                    Log.Error($"Assertion 'NULL' failed, because an object at index '{i}' was null.");
                    throw new AssertionException("NULL", $"an object at index '{i}' was null.");
                }
            }
        }

        public class AssertionException : Exception
        {
            public AssertionException(string name, string message) : base($"Assertion '{name}' failed, because {message}") { }
        }
    }
}