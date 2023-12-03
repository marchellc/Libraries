using System.Net;
using System.Linq;
using System.Threading.Tasks;

using Network.Tcp;
using Network.Extensions;

using Common.Logging;

namespace Test
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Any(c => c.Contains("server")))
            {
                var server = new TcpServer();

                server.TrySetAddress(IPAddress.Any, 7777);

                server.OnConnected += peer =>
                {
                    peer.OnReady += () =>
                    {
                        peer.Transport.OnReady += () =>
                        {
                            peer.Transport.CreateHandler(20, br =>
                            {
                                LogOutput.Common.Info($"Received message: {br.ReadString()}");
                            });

                            peer.Transport.Send(20, bw => bw.Write("there"));
                        };
                    };
                };

                server.Start();
            }
            else
            {
                var client = new TcpClient(new IPEndPoint(IPAddress.Loopback, 7777));

                client.OnConnected += peer =>
                {
                    peer.OnReady += () =>
                    {
                        peer.Transport.OnReady += () =>
                        {
                            peer.Transport.CreateHandler(20, br =>
                            {
                                LogOutput.Common.Info($"Received message: {br.ReadString()}");
                            });

                            peer.Transport.Send(20, bw => bw.Write("hello"));
                        };
                    };
                };

                client.Start();
            }

            await Task.Delay(-1);
        }
    }
}
