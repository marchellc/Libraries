using System.Net;
using System.Linq;
using System.Threading.Tasks;

using Network.Tcp;
using Network.Extensions;

using Common.Logging;

using Network.Synchronization;
using Network.Requests;

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

                server.Features.AddFeature<SynchronizationManager>();
                server.Features.AddFeature<RequestManager>();

                server.OnConnected += peer =>
                {
                    var sync = peer.Features.GetFeature<SynchronizationManager>();

                    sync.CreateHandler<TestRoot>(root =>
                    {
                        LogOutput.Common.Info($"test root created");
                    });
                };

                server.Start();
            }
            else
            {
                var client = new TcpClient(new IPEndPoint(IPAddress.Loopback, 7777));

                client.Features.AddFeature<SynchronizationManager>();
                client.Features.AddFeature<RequestManager>();

                client.OnConnected += peer =>
                {
                    var sync = peer.Features.GetFeature<SynchronizationManager>();

                    sync.OnReady += () =>
                    {
                        var root = sync.Create<TestRoot>();

                        Task.Run(async () =>
                        {
                            for (int i = 0; i < int.MaxValue; i++)
                            {
                                await Task.Delay(1000);
                                root.Word.Value = i;
                            }
                        });
                    };
                };

                client.Start();
            }

            await Task.Delay(-1);
        }
    }

    public class TestRoot : SynchronizedRoot
    {
        public TestRoot()
        {
            LogOutput.Common.Info("Root created!");
        }

        public SynchronizedDelegatedValue<int> Word { get; set; } = new SynchronizedDelegatedValue<int>(br => br.ReadInt32(), (bw, i) => bw.Write(i), 0);
    }
}
