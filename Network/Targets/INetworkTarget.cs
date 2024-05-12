using System.Net;

namespace Network.Targets
{
    public interface INetworkTarget
    {
        string Address { get; }

        int Port { get; }

        IPAddress IPAddress { get; }
        IPEndPoint IPEndPoint { get; }

        bool IsLocal { get; }
    }
}