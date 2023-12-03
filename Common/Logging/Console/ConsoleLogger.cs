using System;

namespace Common.Logging.Console
{
    public class ConsoleLogger : ILogger
    {
        private LogMessage lastMsg;
        private bool isBusy;

        public LogMessage Latest => lastMsg;

        public DateTime Started { get; }

        public ConsoleLogger()
        {
            Started = DateTime.Now;
        }

        public void Emit(LogMessage message)
        {
            while (isBusy)
                continue;

            isBusy = true;

            lastMsg = message;

            try
            {
                if (message.Time != null && message.Time.Length > 0)
                    WriteArray(message.Time);

                if (message.Tag != null && message.Tag.Length > 0)
                    WriteArray(message.Tag);

                if (message.Source != null && message.Source.Length > 0)
                    WriteArray(message.Source);

                if (message.Message != null && message.Message.Length > 0)
                    WriteArray(message.Message);

                System.Console.WriteLine();
            }
            catch { }

            isBusy = false;
        }

        private static void WriteArray(LogCharacter[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (System.Console.ForegroundColor != array[i].Color)
                    System.Console.ForegroundColor = array[i].Color;

                System.Console.Write(array[i].Character);
                System.Console.ResetColor();
            }

            System.Console.Write(' ');
        }
    }
}