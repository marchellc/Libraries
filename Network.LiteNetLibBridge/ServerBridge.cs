using Network.Bridges;
using Network.Controllers;
using Network.Peers;

using System;
using System.Net;
using System.Collections.Generic;

using LiteNetLib;

using Common.Extensions;

namespace Network.LiteNetLibBridge
{
    public class ServerBridge : IBridge
    {
        private readonly EventBasedNetListener m_Listener;
        private readonly NetManager m_Manager;

        private DisconnectReason? m_LastReason;
        private IPEndPoint m_Target;

        public INetworkController Controller { get; }

        public IPEndPoint Target => m_Target;

        public bool IsRunning => m_Manager.IsRunning;
        public bool IsConnected => m_Manager.ConnectedPeersCount > 0;

        public bool RequiresTick => true;
        public bool CanTick => m_Manager.IsRunning;

        public event Action OnStarted;
        public event Action OnStopped;

        public event Action<INetworkPeer> OnConnected;
        public event Action<INetworkPeer, object[]> OnData;
        public event Action<INetworkPeer, DisconnectReason> OnDisconnected;

        public ServerBridge(INetworkController networkController)
        {
            Controller = networkController;

            m_Listener = new EventBasedNetListener();
            m_Manager = new NetManager(m_Listener);

            m_Listener.ConnectionRequestEvent += OnRequest;
            m_Listener.PeerConnectedEvent += OnConnect;
            m_Listener.PeerDisconnectedEvent += OnDisconnect;
            m_Listener.NetworkReceiveEvent += OnReceived;
        }

        public void Connect(IPEndPoint endPoint)
        {
            if (endPoint is null)
                throw new ArgumentNullException(nameof(endPoint));

            if (m_Manager.IsRunning)
                m_Manager.Stop();

            m_Target = endPoint;
            m_Manager.Start(endPoint.Address, null, endPoint.Port);

            OnStarted.Call();
        }

        public void Disconnect(Guid guid, DisconnectReason disconnectReason)
        {
            var peer = LiteTransport.GetPeer(guid);

            if (peer is null)
                return;

            peer.Disconnect();
            m_LastReason = disconnectReason;
        }

        public void Send(Guid guid, IEnumerable<object> data)
        {
            var peer = LiteTransport.GetPeer(guid);

            if (peer is null)
                return;

            var writer = LiteTransport.Write(data);

            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        public void Start()
        {
            if (IsRunning)
                Stop();
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            m_Manager.Stop(true);
            OnStopped.Call();
            LiteTransport.DiscardAll();
        }

        public void Tick()
            => m_Manager.PollEvents();

        private void OnReceived(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            var serverPeer = LiteTransport.GetPeer(peer, this);

            if (serverPeer is null)
                return;

            var serverData = LiteTransport.Read(reader);

            serverPeer.Receive(serverData);
        }

        private void OnDisconnect(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            var serverPeer = LiteTransport.GetPeer(peer, this);

            if (serverPeer is null)
                return;

            OnDisconnected.Call(serverPeer, new DisconnectReason($"Disconnected: {disconnectInfo.Reason} ({disconnectInfo.SocketErrorCode})", false));

            serverPeer.ProcessDisconnect();

            LiteTransport.Discard(peer);
        }

        private void OnConnect(NetPeer peer)
            => OnConnected.Call(LiteTransport.GetPeer(peer, this));

        private void OnRequest(ConnectionRequest request)
            => request.Accept();
    }
}
