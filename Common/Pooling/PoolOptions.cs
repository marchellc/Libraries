using System;

namespace Common.Pooling
{
    [Flags]
    public enum PoolOptions : byte
    {
        None = 0,

        NewOnMissing = 2,
        ExceptionOnMissing = 4,
        DefaultOnMissing = 8,

        EnableTracking = 16,
    }
}