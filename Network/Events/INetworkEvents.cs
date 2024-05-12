using Network.Authentification;
using Network.Data;
using Network.Peers;
using Network.Targets;

using System;

namespace Network.Events
{
    public interface INetworkEvents
    {
        event Action OnStarting;
        event Action OnStarted;

        event Action OnStopping;
        event Action OnStopped;

        event Action<INetworkTarget> OnConnecting;
        event Action<INetworkPeer> OnConnected;

        event Action<INetworkPeer> OnAuthentificated;
        event Action<INetworkPeer> OnAuthentificating;
        event Action<INetworkPeer, AuthentificationFailureReason> OnAuthentificationFailed;

        event Action<INetworkPeer, DataPack> OnData;

        event Action<INetworkPeer, DisconnectReason> OnDisconnecting;
        event Action<INetworkPeer, DisconnectReason> OnDisconnected;

        event Action OnReconnecting;
        event Action OnReconnected;
        event Action OnReconnectionFailed;
    }
}