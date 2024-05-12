using Network.Controllers;
using Network.Peers;

using System;
using System.Collections.Generic;
using System.Net;

namespace Network.Bridges
{
    public interface IBridge
    {
        INetworkController Controller { get; }

        IPEndPoint Target { get; }

        bool IsRunning { get; }
        bool IsConnected { get; }

        bool RequiresTick { get; }
        bool CanTick { get; }

        event Action OnStarted;
        event Action OnStopped;

        event Action<INetworkPeer> OnConnected;
        event Action<INetworkPeer, object[]> OnData;
        event Action<INetworkPeer, DisconnectReason> OnDisconnected;

        void Start();
        void Stop();

        void Connect(IPEndPoint endPoint);
        void Disconnect(Guid guid, DisconnectReason disconnectReason);

        void Tick();

        void Send(Guid guid, IEnumerable<object> data);
    }
}