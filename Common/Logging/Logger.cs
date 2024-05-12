﻿using Common.Utilities;

namespace Common.Logging
{
    public static class Logger
    {
        public static readonly LogOutput Output = new LogOutput().Setup();

        public static void Info(object msg)
            => Output.Info(GetCaller(), msg);

        public static void Trace(object msg)
            => Output.Trace(GetCaller(), msg);

        public static void Verbose(object msg)
            => Output.Verbose(GetCaller(), msg);

        public static void Debug(object msg)
            => Output.Debug(GetCaller(), msg);

        public static void Error(object msg)
            => Output.Error(GetCaller(), msg);

        public static void Warn(object msg)
            => Output.Warn(GetCaller(), msg);

        public static void Fatal(object msg)
            => Output.Fatal(GetCaller(), msg);

        public static string GetCaller(int skipFrames = 0)
        {
            var method = CodeUtils.ResolveCaller(skipFrames + 1);

            if (method != null)
                return $"{method.DeclaringType.Name} / {method.Name}";

            return "unknown";
        }
    }
}
