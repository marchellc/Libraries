using Networking.Enums;

using System.Net;

namespace Networking.Interfaces
{
    public interface IClient : IManager
    {
        IPEndPoint LocalAddress { get; }
        IPEndPoint RemoteAddress { get; }

        ISender Sender { get; }

        ClientType Type { get; }

        bool IsRunning { get; }
        bool IsConnected { get; }

        void Start();
        void Stop();

        void Send(byte[] data);
    }
}