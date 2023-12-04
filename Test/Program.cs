using System.IO;
using System.Net;
using System.Linq;
using System.Threading.Tasks;

using Network.Tcp;
using Network.Extensions;

using Network.Synchronization;
using Network.Requests;
using Network.Interfaces.Transporting;
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

                server.Features.AddFeature<SynchronizationManager>();
                server.Features.AddFeature<RequestManager>();

                server.OnConnected += peer =>
                {
                    var req = peer.Features.GetFeature<RequestManager>();

                    req.CreateHandler<TestRequestMessage>((reqInfo, msg) =>
                    {
                        reqInfo.Success(new TestResponseMessage { Number = msg.Number + 5 });
                    });
                };

                server.Start();
            }
            else
            {
                var client = new TcpClient(new IPEndPoint(IPAddress.Loopback, 7777));
                var number = 5;
                var expected = 10;

                client.Features.AddFeature<SynchronizationManager>();
                client.Features.AddFeature<RequestManager>();

                client.OnConnected += peer =>
                {
                    var req = peer.Features.GetFeature<RequestManager>();

                    req.Request<TestRequestMessage, TestResponseMessage>(new TestRequestMessage { Number = number }, 0, (res, msg) => 
                    {
                        if (msg.Number != expected)
                            LogOutput.Common.Error("Not the expected number");
                        else
                            LogOutput.Common.Info("Expected number");
                    });
                };

                client.Start();
            }

            await Task.Delay(-1);
        }
    }

    public struct TestRequestMessage : IMessage
    {
        public int Number;

        public void Read(BinaryReader reader, ITransport transport)
        {
            Number = reader.ReadInt32();
        }

        public void Write(BinaryWriter writer, ITransport transport)
        {
            writer.Write(Number);
        }
    }

    public struct TestResponseMessage : IMessage
    {
        public int Number;

        public void Read(BinaryReader reader, ITransport transport)
        {
            Number = reader.ReadInt32();
        }

        public void Write(BinaryWriter writer, ITransport transport)
        {
            writer.Write(Number);
        }
    }
}
