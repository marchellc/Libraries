using Common.Logging;
using Common.Utilities;

using Networking.Client;
using Networking.Server;

using System.Threading.Tasks;
using System.Net;

using Networking.Entities;
using Networking.Entities.Attributes;
using Networking;

namespace Test
{
    public static class Program
    {
        public static LogOutput log;

        public static async Task Main(string[] args)
        {
            log = new LogOutput("Test").Setup();

            var port = int.Parse(ConsoleArgs.GetValue("port"));

            if (ConsoleArgs.HasSwitch("server"))
            {
                NetServer.Instance.Port = port;
                NetServer.Instance.Add<NetEntityManager>();
                NetServer.Instance.Start();
            }
            else
            {
                NetClient.Instance.Add<NetEntityManager>();
                NetClient.Instance.Add<TestComponent>();
                NetClient.Instance.Connect(new IPEndPoint(IPAddress.Loopback, port));
            }

            await Task.Delay(-1);
        }
    }

    public class TestComponent : NetComponent
    {
        public override void Start()
        {
            base.Start();
            Client.Get<NetEntityManager>().SpawnEntity<TestClientEntity>();
        }
    }

    [NetEntityRemoteType("TestServerEntity")]
    public class TestClientEntity : NetEntity
    {
        private static ushort NetworkTestCode;
        private TestEnum NetworkTestDefValue = TestEnum.value1;

        public TestEnum NetworkTest
        {
            get => GetSync<TestEnum>(NetworkTestCode);
            set => SetSync(NetworkTestCode, value);
        }

        public override void OnSpawnConfirmed()
        {
            base.OnSpawnConfirmed();
            Program.log.Info("Spawn confirmed");
            NetworkTest = TestEnum.value2;
        }

        public override void OnSpawned(NetEntityRequestType netEntityRequestType = NetEntityRequestType.LocalRequest)
        {
            base.OnSpawned(netEntityRequestType);
            Program.log.Info("Spawned");
        }
    }

    [NetEntityRemoteType("TestClientEntity")]
    public class TestServerEntity : NetEntity
    {
        private static ushort NetworkTestCode;

        public TestEnum NetworkTest
        {
            get => GetSync<TestEnum>(NetworkTestCode);
            set => SetSync(NetworkTestCode, value);
        }

        public override void OnSpawnConfirmed()
        {
            base.OnSpawnConfirmed();
            Program.log.Info("Spawn confirmed");
        }

        public override void OnSpawned(NetEntityRequestType netEntityRequestType = NetEntityRequestType.LocalRequest)
        {
            base.OnSpawned(netEntityRequestType);
            Program.log.Info("Spawned");
        }
    }

    public enum TestEnum : byte
    {
        value1 = 0,
        value2 = 2
    }
}