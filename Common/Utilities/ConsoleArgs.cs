using Common.IO.Collections;
using Common.Logging;

using System;

namespace Common.Utilities
{
    public static class ConsoleArgs
    {
        private static LockedDictionary<string, string> keys = new LockedDictionary<string, string>();
        private static LockedList<string> switches = new LockedList<string>();

        public static void Parse(string[] args)
        {
            if (args is null || args.Length <= 0)
                return;

            for (int i = 0; i < args.Length; i++)
            {
                if (string.IsNullOrEmpty(args[i]))
                    continue;

                if (args[i].StartsWith("--"))
                {
                    if (!args[i].Contains("="))
                    {
                        LogOutput.Common?.Warn($"Failed to parse argument '{args[i]}' at {i}");
                        continue;
                    }

                    var split = args[i].Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                    if (split.Length != 2)
                    {
                        LogOutput.Common?.Warn($"Failed to parse argument '{args[i]}' at {i}");
                        continue;
                    }

                    var key = split[0].Replace("--", "").Trim();
                    var value = split[1].Trim();

                    if (keys.ContainsKey(key))
                    {
                        LogOutput.Common?.Warn($"Failed to parse argument '{args[i]}' at {i}: this argument already exists");
                        continue;
                    }

                    keys[key] = value;

                    LogOutput.Common?.Trace($"Loaded argument: {key} ({value})");
                }
                else if (args[i].StartsWith("-"))
                {
                    var switchName = args[i].Trim('-');

                    if (switches.Contains(switchName))
                    {
                        LogOutput.Common?.Warn($"Failed to parse argument '{args[i]}' at {i}: this switch already exists");
                        continue;
                    }

                    switches.Add(switchName);

                    LogOutput.Common?.Trace($"Loaded switch: {switchName}");
                }
                else
                {
                    LogOutput.Common?.Warn($"Failed to parse argument '{args[i]}' at {i}");
                    continue;
                }
            }

            LogOutput.Common?.Info($"Parsed {keys.Count} key(s) and {switches.Count} switch(es) from {args.Length} startup argument(s).");
        }

        public static bool HasSwitch(string switchName)
            => switches.Contains(switchName);

        public static string GetValue(string key)
            => keys.TryGetValue(key, out var value) ? value : null;
    }
}
