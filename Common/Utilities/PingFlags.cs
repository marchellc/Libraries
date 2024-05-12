using System;

namespace Common.Utilities
{
    [Flags]
    public enum PingFlags : byte
    {
        None = 0,
        DontFragment = 2
    }
}