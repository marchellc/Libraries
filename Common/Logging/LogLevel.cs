namespace Common.Logging
{
    public enum LogLevel : byte
    {
        Trace = 0,
        Debug = 2,
        Verbose = 4,

        Information = 6,

        Warning = 8,

        Error = 10,
        Fatal = 12
    }
}