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

        public IPInfo(IPType type, int port, IPAddress address)
        {
            this.type = type;
            this.port = port;
            this.address = address;

            this.isLocal = type is IPType.Local;
            this.isRemote = type is IPType.Remote;

            this.raw = address.ToString();
            this.endPoint = new IPEndPoint(address, port);
        }

        public override string ToString()
            => endPoint.ToString();

        public override int GetHashCode()
            => endPoint.GetHashCode();
    }
}