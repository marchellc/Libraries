using System.Net;

namespace Network.Targets.Ip
{
    public struct IPTarget : INetworkTarget
    {
        public string Address { get; }
        public int Port { get; }

        public IPAddress IPAddress { get; }
        public IPEndPoint IPEndPoint { get; }

        public bool IsLocal
        {
            get => IPAddress.IsLoopback(IPAddress) || IPAddress == IPAddress.Any;
        }

        public IPTarget(string address, int port)
        {
            Address = address;
            Port = port;

            IPAddress = (IsLocalTarget(address) ? IPAddress.Loopback : IPAddress.Parse(address));
            IPEndPoint = new IPEndPoint(IPAddress, port);
        }

        public IPTarget(IPAddress address, int port) : this(address.ToString(), port)
        {
            Address = address.ToString();
            Port = port;

            IPAddress = address;
            IPEndPoint = new IPEndPoint(address, port);
        }

        public IPTarget(IPEndPoint endPoint)
        {
            Address = endPoint.Address.ToString();
            Port = endPoint.Port;

            IPAddress = endPoint.Address;
            IPEndPoint = endPoint;
        }

        public static bool IsLocalTarget(string address)
            => address != null && (address == "local" || address == "localhost" || address == "127.0.0.1" || address == "0.0.0.0");

        public static INetworkTarget GetLocalLoopback(int port = 0)
            => new IPTarget(IPAddress.Loopback, port);

        public static INetworkTarget GetLocalAny(int port = 0)
            => new IPTarget(IPAddress.Any, port);

        public static INetworkTarget Get(string address)
        {
            var parts = address.Split(':');

            var ip = parts[0];
            var port = parts[1];

            return new IPTarget(address, int.Parse(port));
        }
    }
}