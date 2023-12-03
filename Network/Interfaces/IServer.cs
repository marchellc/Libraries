using System;

namespace Network.Interfaces
{
    public interface IServer : IController
    {
        public event Action<IPeer> OnConnected;
        public event Action<IPeer> OnDisconnected;
        public event Action<IPeer, ITransport, byte[]> OnData;

        bool IsActive { get; }

        void Send(int connectionId, byte[] data);
        void Disconnect(int connectionId);

        void AddFeature<T>() where T : IFeature;
        void RemoveFeature<T>() where T : IFeature;
    }
}