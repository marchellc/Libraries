using System.Net;

namespace Networking.Address
{
    public struct IPInfo
    {
        public readonly IPType type;

        public readonly string raw;

        public readonly int port;

        public readonly bool isLocal;
        public readonly bool isRemote;

        public readonly IPAddress address;
        public readonly IPEndPoint endPoint;

        public IPInfo(IPType type, string raw, int port, IPAddress address)
        {
            this.type = type;
            this.raw = raw;
            this.port = port;
            this.address = address;

            this.isLocal = type is IPType.Local;
            this.isRemote = type is IPType.Remote;

            this.endPoint = new IPEndPoint(address, port);
        }
    }
}