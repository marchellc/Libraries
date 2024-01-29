using Common.Logging;

using System.Threading.Tasks;

using Common.Utilities;

using Networking.Server;
using Networking.Components;
using Networking.Client;
using Networking.Requests;

namespace Test
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var log = new LogOutput("Test").Setup();

            if (ConsoleArgs.HasSwitch("client"))
            {
                var client = NetworkClient.Instance;

                client.Add<NetworkParent>();

                client.OnConnected += () =>
                {
                    var parent = client.Get<NetworkParent>();

                    parent.OnObjectSpawned += (_, obj, type) =>
                    {
                        if (obj is RequestManager requestManager)
                        {
                            requestManager.Listen<string>((req, str) =>
                            {
                                log.Info($"str: {str}");
                                req.RespondOk(str + str);
                            });
                        }
                    };
                };

                client.Connect();
            }
            else
            {
                var server = NetworkServer.Instance;

                server.Add<NetworkParent>();

                server.OnConnected += conn =>
                {
                    var parent = conn.Get<NetworkParent>();

                    parent.SpawnObject<RequestManager>(parent.Identity, NetworkRequestType.Current, req =>
                    {
                        req.Request<string>("test", (res, str) =>
                        {
                            log.Info($"str: {str}");
                        });
                    });

                };

                server.Start();
            }

            await Task.Delay(-1);
        }
    }
}