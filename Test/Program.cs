using Common.Logging;
using Common.Utilities;

using Networking.Client;
using Networking.Server;

using System.Threading.Tasks;
using System.Net;

namespace Test
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var log = new LogOutput("Test").Setup();
            var port = int.Parse(ConsoleArgs.GetValue("port"));

            if (ConsoleArgs.HasSwitch("server"))
            {
                NetServer.Instance.Port = port;
                NetServer.Instance.Start();
            }
            else
            {
                NetClient.Instance.Connect(new IPEndPoint(IPAddress.Loopback, port));
            }

            await Task.Delay(-1);
        }
    }
}