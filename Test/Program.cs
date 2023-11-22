using Network.Logging;
using Network.Tcp;

using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;

namespace Test
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            NetworkLog.OnLog += (level, tag, msg) =>
            {
                switch (level)
                {
                    case NetworkLogLevel.TelepathyWarning:
                    case NetworkLogLevel.Warning:
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"[{level}] {tag}: {msg}");
                            Console.ResetColor();
                            break;
                        }

                    case NetworkLogLevel.TelepathyInfo:
                    case NetworkLogLevel.Info:
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"[{level}] {tag}: {msg}");
                            Console.ResetColor();
                            break;
                        }

                    case NetworkLogLevel.TelepathyDebug:
                    case NetworkLogLevel.Debug:
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"[{level}] {tag}: {msg}");
                            Console.ResetColor();
                            break;
                        }

                    case NetworkLogLevel.TelepathyError:
                    case NetworkLogLevel.Error:
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[{level}] {tag}: {msg}");
                            Console.ResetColor();
                            break;
                        }
                }
            };

            if (args.Any(c => c.Contains("server")))
            {
                var server = new TcpServer(7777);

                server.Start();
                server.Connect();
            }
            else
            {
                var client = new TcpClient(new IPEndPoint(IPAddress.Loopback, 7777));

                client.Start();
                client.Connect();
            }

            await Task.Delay(-1);
        }
    }
}
