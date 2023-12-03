using System;

namespace Network
{
    public interface IClient : IController
    {
        bool IsConnected { get; }

        IPeer Peer { get; }
        ITransport Transport { get; }

        public event Action<IPeer> OnConnected;
        public event Action<IPeer> OnDisconnected;
        public event Action<IPeer, ITransport, byte[]> OnData;

        void Send(byte[] data);
    }
}