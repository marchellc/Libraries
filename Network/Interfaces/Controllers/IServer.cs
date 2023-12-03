using System;

using Network.Interfaces.Features;
using Network.Interfaces.Transporting;

namespace Network.Interfaces.Controllers
{
    public interface IServer : IController
    {
        public event Action<IPeer> OnConnected;
        public event Action<IPeer> OnDisconnected;
        public event Action<IPeer, ITransport, byte[]> OnData;

        bool IsActive { get; }

        void Send(int connectionId, byte[] data);
        void Disconnect(int connectionId);
    }
}