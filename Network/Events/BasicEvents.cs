using Common.Extensions;

using Network.Authentification;
using Network.Data;
using Network.Peers;
using Network.Targets;

using System;

namespace Network.Events
{
    public class BasicEvents : INetworkEvents
    {
        public event Action OnStarting;
        public event Action OnStarted;

        public event Action OnStopping;
        public event Action OnStopped;

        public event Action<INetworkTarget> OnConnecting;
        public event Action<INetworkPeer> OnConnected;

        public event Action<INetworkPeer> OnAuthentificated;
        public event Action<INetworkPeer> OnAuthentificating;
        public event Action<INetworkPeer, AuthentificationFailureReason> OnAuthentificationFailed;

        public event Action<INetworkPeer, DataPack> OnData;

        public event Action<INetworkPeer, DisconnectReason> OnDisconnecting;
        public event Action<INetworkPeer, DisconnectReason> OnDisconnected;

        public event Action OnReconnecting;
        public event Action OnReconnected;
        public event Action OnReconnectionFailed;

        public void Starting()
            => OnStarting.Call();

        public void Started()
            => OnStarted.Call();

        public void Stopping()
            => OnStopping.Call();

        public void Stopped()
            => OnStopped.Call();

        public void Connecting(INetworkTarget networkTarget)
            => OnConnecting.Call(networkTarget);

        public void Connected(INetworkPeer networkPeer)
            => OnConnected.Call(networkPeer);

        public void Authentificated(INetworkPeer networkPeer)
            => OnAuthentificated.Call(networkPeer);

        public void Authentificating(INetworkPeer networkPeer)
            => OnAuthentificating.Call(networkPeer);

        public void AuthentificationFailed(INetworkPeer networkPeer, AuthentificationFailureReason authentificationFailureReason)
            => OnAuthentificationFailed.Call(networkPeer, authentificationFailureReason);

        public void Data(INetworkPeer networkPeer, DataPack dataPack)
            => OnData.Call(networkPeer, dataPack);

        public void Disconnecting(INetworkPeer networkPeer, DisconnectReason disconnectReason)
            => OnDisconnecting.Call(networkPeer, disconnectReason);

        public void Disconnected(INetworkPeer networkPeer, DisconnectReason disconnectReason)
            => OnDisconnected.Call(networkPeer, disconnectReason);

        public void Reconnecting()
            => OnReconnecting.Call();

        public void Reconnected()
            => OnReconnected.Call();

        public void ReconnectionFailed()
            => OnReconnectionFailed.Call();
    }
}