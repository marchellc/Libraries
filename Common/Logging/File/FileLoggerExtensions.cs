namespace Common.Logging.File
{
    public static class FileLoggerExtensions
    {
        public static LogOutput AddFileWithPrefix(this LogOutput output, string logFileNamePrefix)
        {
            output.AddLogger(new FileLogger(LogUtils.GetFilePath(logFileNamePrefix)));
            return output;
        }

        public static LogOutput AddFileFromOutput(this LogOutput output, LogOutput other)
        {
            if (other is null)
                return output;
            
            var otherFile = other.GetLogger<FileLogger>();

            if (otherFile is null)
                return output;

            output.AddLogger(otherFile);
            return output;
        }
    }
}
