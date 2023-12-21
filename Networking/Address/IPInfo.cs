using System.Net;

namespace Networking.Address
{
    public struct IPInfo
    {
        public readonly IPType Type;

        public readonly string Raw;

        public readonly int Port;

        public readonly bool IsLocal;
        public readonly bool IsRemote;

        public readonly IPAddress Address;
        public readonly IPEndPoint EndPoint;

        public IPInfo(IPType type, string raw, int port, IPAddress address)
        {
            this.Type = type;
            this.Raw = raw;
            this.Port = port;
            this.Address = address;

            this.IsLocal = type is IPType.Local;
            this.IsRemote = type is IPType.Remote;

            this.EndPoint = new IPEndPoint(address, port);
        }
    }
}