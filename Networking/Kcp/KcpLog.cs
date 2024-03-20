using Common.Logging;

using System;

namespace Networking.Kcp
{
    public static class KcpLog
    {
        public static readonly LogOutput Log = new LogOutput("KcpLog").Setup();

        public static Action<string> Info    = Log.Verbose;
        public static Action<string> Warning = Log.Warn;
        public static Action<string> Error   = Log.Error;
    }
}