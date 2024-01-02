using Common.Logging;

using Common.Utilities;

using Networking.Client;
using Networking.Data;
using Networking.Objects;
using Networking.Requests;
using Networking.Server;

using System.Net;
using System.Threading.Tasks;

namespace Test
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var log = LogOutput.Common;
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

                NetworkServer.instance.port = serverPortValue;

                NetworkServer.instance.Add<NetworkManager>();

                NetworkServer.instance.Start();

                NetworkServer.instance.OnConnected += conn =>
                {
                    CodeUtils.Delay(() =>
                    {
                        var netManager = conn.Get<NetworkManager>();

                        CodeUtils.Delay(() =>
                        {
                            var testObj = netManager.Instantiate<TestObject>();
                        }, 200);
                    }, 200);
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

                NetworkClient.instance.Add<NetworkManager>();
                NetworkClient.instance.Connect(new IPEndPoint(IPAddress.Loopback, clientPortValue));
                NetworkClient.instance.OnConnected += () =>
                {
                    var netManager = NetworkClient.instance.Get<NetworkManager>();
                    var testObj = netManager.Instantiate<TestObject>();
                };
            }

            await Task.Delay(-1);
        }
    }

    public class TestObject : NetworkObject
    {
        public TestObject(NetworkManager manager) : base(manager)
        {
        }

        public NetworkList<string> networkListTest;
        public int prevSize = 0;
        public bool isRemoved;

        public override void OnStart()
        {
            CodeUtils.WhileTrue(() => !isDestroyed && isReady, () =>
            {
                if (net.isServer)
                {
                    if (networkListTest.Count >= 10)
                    {
                        if (!isRemoved)
                        {
                            var randomIndex = Generator.Instance.GetInt32(0, networkListTest.Count);
                            LogOutput.Common.Verbose($"Removing item at random index");
                            var size = networkListTest.Count;
                            networkListTest.RemoveAt(randomIndex);
                            LogOutput.Common.Verbose($"After removal: {size} ({networkListTest.Count})");
                            isRemoved = true;
                        }
                        else
                        {
                            LogOutput.Common.Info($"Clearing list");
                            networkListTest.Clear();
                            LogOutput.Common.Info($"Count: {networkListTest.Count}");
                            isRemoved = false;
                        }

                        return;
                    }

                    var value = Generator.Instance.GetString();
                    networkListTest.Add(value);
                    LogOutput.Common.Verbose($"Added value to list: {value} ({networkListTest.Count})");
                }
                else
                {
                    if (networkListTest.Count != prevSize)
                    {
                        LogOutput.Common.Verbose($"List size changed (from {prevSize} to {networkListTest.Count})");

                        prevSize = networkListTest.Count;

                        for (int i = 0; i < networkListTest.Count; i++)
                            LogOutput.Common.Verbose($"{networkListTest[i]}");
                    }
                    else
                    {
                        LogOutput.Common.Verbose($"List has not changed");
                    }
                }
            }, 1500);
        }
    }
}