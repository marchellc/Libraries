using System;
using System.Collections.Generic;
using System.Threading;

namespace Common.Logging.File
{
    public class FileLogger : ILogger
    {
        private string path;
        private object gLock;
        private Timer timer;

        private LogMessage lastMsg;

        private List<LogMessage> toAdd;

        public LogMessage Latest => lastMsg;

        public DateTime Started { get; }

        public FileLogger(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            this.path = path;
            this.gLock = new object();
            this.toAdd = new List<LogMessage>();
            this.timer = new Timer(_ => WriteLogs(), null, 10, 500);
        }

        ~FileLogger()
        {
            timer.Dispose();
            timer = null;
        }

        public void Emit(LogMessage message)
        {
            lastMsg = message;

            lock (gLock)
                toAdd.Add(message);
        }

        private void WriteLogs()
        {
            lock (gLock)
            {
                try
                {
                    foreach (var msg in toAdd)
                    {
                        var str = msg.GetString();

                        if (string.IsNullOrWhiteSpace(str))
                            continue;

                        System.IO.File.AppendAllLines(path, new string[] { str });
                    }
                }
                catch { }

                toAdd.Clear();
            }
        }
    }
}