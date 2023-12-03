using Network.Interfaces;

using System;

namespace Network
{
    public interface IPeer : IController, IFeatures
    {
        int Id { get; }

        bool IsConnected { get; }

        ITransport Transport { get; }

        public event Action OnReady;

        void Disconnect();
    }
}