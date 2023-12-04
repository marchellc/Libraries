using Common.Pooling.Pools;

using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Logging.File
{
    public class FileLogger : ILogger
    {
        private string path;
        private object gLock;

        private int maxSize = 12;

        private LogMessage lastMsg;

        private List<LogMessage> toAdd;

        public LogMessage Latest => lastMsg;

        public DateTime Started { get; }

        public FileLogger(string path, int maxSize = 12)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            this.path = path;
            this.maxSize = maxSize;
            this.gLock = new object();
            this.toAdd = new List<LogMessage>();
        }

        public void Emit(LogMessage message)
        {
            lastMsg = message;

            lock (gLock)
                toAdd.Add(message);

            if (toAdd.Count >= maxSize)
                WriteLogs();
        }

        private void WriteLogs()
        {
            lock (gLock)
            {
                try
                {
                    foreach (var msg in toAdd)
                    {
                        var str = MakeString(msg);

                        if (string.IsNullOrWhiteSpace(str))
                            continue;

                        System.IO.File.AppendAllLines(path, new string[] { str });
                    }
                }
                catch { }
            }
        }

        private static string MakeString(LogMessage message)
        {
            var str = StringBuilderPool.Shared.Next();

            if (message.Time != null && message.Time.Length > 0)
                WriteArray(message.Time, str);

            if (message.Tag != null && message.Tag.Length > 0)
                WriteArray(message.Tag, str);

            if (message.Source != null && message.Source.Length > 0)
                WriteArray(message.Source, str);

            if (message.Message != null && message.Message.Length > 0)
                WriteArray(message.Message, str);

            return StringBuilderPool.Shared.StringReturn(str);
        }

        private static void WriteArray(LogCharacter[] array, StringBuilder str)
        {
            for (int i = 0; i < array.Length; i++)
                str.Append(array[i].Character);

            str.Append(' ');
        }
    }
}