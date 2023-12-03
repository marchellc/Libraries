using System.IO;
using System.Net;
using System.Linq;
using System.Threading.Tasks;

using Network.Tcp;
using Network.Extensions;

using Common.Logging;
using Network.Synchronization;

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
                    var sync = peer.AddFeature<SynchronizationManager>();

                    sync.CreateHandler<TestRoot>(root =>
                    {
                        LogOutput.Common.Info($"test root created");
                    });

                    LogOutput.Common.Info("sync ready");
                };

                server.Start();
            }
            else
            {
                var client = new TcpClient(new IPEndPoint(IPAddress.Loopback, 7777));

                client.OnConnected += peer =>
                {
                    var sync = peer.AddFeature<SynchronizationManager>();

                    sync.Create<TestRoot>().Word.Value = "there";
                };

                client.Start();
            }

            await Task.Delay(-1);
        }
    }

    public class TestRoot : SynchronizedRoot
    {
        public SynchronizedString Word { get; } = new SynchronizedString("hello");
    }
}
