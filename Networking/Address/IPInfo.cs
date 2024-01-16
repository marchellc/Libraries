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

        public IPInfo(IPType type, int port, IPAddress address)
        {
            Type = type;
            Port = port;
            Address = address;

            IsLocal = type is IPType.Local;
            IsRemote = type is IPType.Remote;

            Raw = address.ToString();
            EndPoint = new IPEndPoint(address, port);
        }

        public override string ToString()
            => EndPoint.ToString();

        public override int GetHashCode()
            => EndPoint.GetHashCode();
    }
}