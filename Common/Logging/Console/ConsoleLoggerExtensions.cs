namespace Common.Logging.Console
{
    public static class ConsoleLoggerExtensions
    {
        public static LogOutput AddConsoleIfPresent(this LogOutput output)
        {
            if (LogUtils.IsConsoleAvailable)
                output.AddLogger(ConsoleLogger.Instance);

            return output;
        }
    }
}