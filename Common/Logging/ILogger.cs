using System;

namespace Common.Logging
{
    public interface ILogger
    {
        DateTime Started { get; }

        LogMessage Latest { get; }

        void Emit(LogMessage message);
    }
}