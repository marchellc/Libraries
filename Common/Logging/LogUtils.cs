using Common.Logging.Console;
using Common.Logging.File;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Common.Logging
{
    public static class LogUtils
    {
        private static bool consChecked;
        private static bool consAvailable;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        public static bool IsConsoleAvailable
        {
            get
            {
                if (consChecked)
                    return consAvailable;

                consChecked = true;

                try
                {
                    consAvailable = GetConsoleWindow() != IntPtr.Zero;
                }
                catch { }

                return consAvailable;
            }
        }

        public static string Time
        {
            get
            {
                var time = DateTime.Now;
                return $"{time.Hour}:{time.Minute}:{time.Second}:{time.Millisecond}";
            }
        }

        public static string TimeFileName
        {
            get
            {
                var time = DateTime.Now;
                return $"{time.Day}_{time.Month}_{time.Year} {time.Hour}_{time.Minute}";
            }
        }

        public static LogOutput Setup(this LogOutput output, bool includeFile = true)
        {
            output.AddConsoleIfPresent();

            if (includeFile)
                output.AddFileFromOutput(LogOutput.Common);

            return output;
        }

        public static string GetFilePath(string fileNamePrefix)
        {
            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var genLogDir = $"{appDataDir}/common_library_logs";

            if (!Directory.Exists(genLogDir))
                Directory.CreateDirectory(genLogDir);

            var logDir = $"{genLogDir}/{ModuleInitializer.GetAppName()}";

            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            return $"{logDir}/{fileNamePrefix}-{TimeFileName}.txt";
        }

        public static LogMessage CreateMessage(string source, string message, LogLevel level)
        {
            var color = ColorOfLevel(level);
            var inverse = InverseColorOfLevel(level);
            var timeArray = CreateTag(Time, color, inverse);
            var levelArray = CreateTag(NameOfLevel(level), color, inverse);
            var sourceArray = CreateTag(source, color, inverse);

            var msgArray = new LogCharacter[message.Length];

            for (int i = 0; i < message.Length; i++)
                msgArray[i] = new LogCharacter(message[i], level > LogLevel.Warning ? color : ConsoleColor.White);

            return new LogMessage
            {
                Level = level,

                RequestTime = DateTime.Now,
                ResponseTime = DateTime.Now,

                Message = msgArray,
                Source = sourceArray,
                Tag = levelArray,
                Time = timeArray
            };
        }

        public static LogCharacter[] CreateTag(string tag, LogLevel level)
        {
            var chars = new LogCharacter[tag.Length + 2];

            var color = ColorOfLevel(level);
            var inverse = InverseColorOfLevel(level);

            chars[0] = new LogCharacter('[', inverse);
            chars[chars.Length - 1] = new LogCharacter(']', inverse);

            for (int i = 0; i < tag.Length; i++)
                chars[i + 1] = new LogCharacter(tag[i], color);

            return chars;
        }

        public static LogCharacter[] CreateTag(string tag, ConsoleColor color, ConsoleColor inverse)
        {
            var chars = new LogCharacter[tag.Length + 2];

            chars[0] = new LogCharacter('[', inverse);
            chars[chars.Length - 1] = new LogCharacter(']', inverse);

            for (int i = 0; i < tag.Length; i++)
                chars[i + 1] = new LogCharacter(tag[i], color);

            return chars;
        }

        public static string NameOfLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Fatal:
                    return "FATAL";

                case LogLevel.Error:
                    return "ERROR";

                case LogLevel.Warning:
                    return "WARN";

                case LogLevel.Information:
                    return "INFO";

                case LogLevel.Verbose:
                    return "VERBOSE";

                case LogLevel.Debug:
                    return "DEBUG";

                case LogLevel.Trace:
                    return "TRACE";

                default:
                    return "??";
            }
        }

        // text
        public static ConsoleColor ColorOfLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Fatal:
                    return ConsoleColor.DarkRed;

                case LogLevel.Error:
                    return ConsoleColor.White;

                case LogLevel.Warning:
                    return ConsoleColor.Yellow;

                case LogLevel.Information:
                    return ConsoleColor.White;

                case LogLevel.Verbose:
                    return ConsoleColor.White;

                case LogLevel.Debug:
                    return ConsoleColor.White;

                case LogLevel.Trace:
                    return ConsoleColor.White;

                default:
                    return ConsoleColor.White;
            }
        }

        // brackets
        public static ConsoleColor InverseColorOfLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Fatal:
                    return ConsoleColor.Red;
                
                case LogLevel.Error:
                    return ConsoleColor.DarkRed;

                case LogLevel.Warning:
                    return ConsoleColor.DarkYellow;

                case LogLevel.Information:
                    return ConsoleColor.DarkBlue;

                case LogLevel.Verbose:
                    return ConsoleColor.Cyan;

                case LogLevel.Debug:
                    return ConsoleColor.Blue;

                case LogLevel.Trace:
                    return ConsoleColor.Magenta;

                default:
                    return ConsoleColor.Magenta;
            }
        }
    }
}
