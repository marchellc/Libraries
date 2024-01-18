using Common.Logging;

using Common.Utilities;

using Networking.Client;
using Networking.Objects;
using Networking.Pinging;
using Networking.Server;

using System;
using System.Net;
using System.Threading.Tasks;

namespace Test
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var log = LogOutput.Common;

            /*
            var isServer = ConsoleArgs.HasSwitch("server");

            if (isServer)
            {
                log.Info("Starting server ..");

                var serverPort = ConsoleArgs.GetValue("port");

                if (!int.TryParse(serverPort, out var serverPortValue))
                {
                    log.Info("Invalid server port");
                    await Task.Delay(-1);
                }

                log.Info($"Port: {serverPortValue}");

                NetworkServer.Instance.Port = serverPortValue;

                NetworkServer.Instance.Add<NetworkManager>();
                NetworkServer.Instance.Start();

                NetworkServer.Instance.OnConnected += conn =>
                {
                    CodeUtils.Delay(() =>
                    {
                        var netManager = conn.Get<NetworkManager>();

                        netManager.OnInitialized += () =>
                        {
                            var testObj = netManager.Get<TestObject>();

                            testObj.Raise();
                        };
                    }, 100);
                };
            }
            else
            {
                log.Info("Starting client ..");

                var clientPort = ConsoleArgs.GetValue("port");

                if (!int.TryParse(clientPort, out var clientPortValue))
                {
                    log.Info("Invalid client port");
                    await Task.Delay(-1);
                }

                log.Info($"Port: {clientPortValue}");

                NetworkClient.Instance.Add<NetworkManager>();
                NetworkClient.Instance.Connect(new IPEndPoint(IPAddress.Loopback, clientPortValue));
            }
            */

            ConsoleCommands.Add("ping", cmdArgs =>
            {
                if (cmdArgs.Length != 1)
                    return "Missing arguments! ping <host>";

                PingUtils.PingThread(cmdArgs[0], res => log.Info(res), 50, 100);

                return "Ping in progress ..";
            });

            await Task.Delay(-1);
        }
    }

    public class TestObject : NetworkObject
    {
        public TestObject(NetworkManager manager) : base(manager)
        {
        }

        public event Action NetworkEventTest;
        public event Action<string> NetworkStringEventTest;

        private static ushort NetworkEventTestHash;
        private static ushort NetworkStringEventTestHash;

        public override void OnStart()
        {
            NetworkEventTest += OnEvent;
            NetworkStringEventTest += OnStringEvent;
        }

        public void Raise()
        {
            SendEvent(NetworkEventTestHash, true);
            SendEvent(NetworkStringEventTestHash, true, Generator.Instance.GetString());
        }

        private void OnStringEvent(string str)
        {
            manager.Log.Info($"String Event: {str}");
        }

        private void OnEvent()
        {
            manager.Log.Info("Event");
        }
    }
}