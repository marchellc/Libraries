using System;

namespace Common.Utilities.Exceptions
{
    [Flags]
    public enum ExceptionSettings : byte
    {
        None = 2,
        LogHandled = 4,
        LogUnhandled = 8
    }
}