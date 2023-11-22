using Common.Reflection;

using System;

namespace Network.Logging
{
    public static class NetworkLog
    {
        public static event Action<NetworkLogLevel, string, string> OnLog;

        public static void Add(NetworkLogLevel level, string tag, object value)
            => OnLog.Call(level, tag, value.ToString());
    }
}