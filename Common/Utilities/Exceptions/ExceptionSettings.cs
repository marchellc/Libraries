using System;

namespace Common.Utilities.Exceptions
{
    [Flags]
    public enum ExceptionSettings : byte
    {
        None = 0,

        LogHandled,
        LogUnhandled
    }
}