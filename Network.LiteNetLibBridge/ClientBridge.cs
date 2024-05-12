using LiteNetLib;

using Network.Bridges;
using Network.Controllers;
using Network.Peers;

using System;
using System.Net;
using System.Collections.Generic;

using Common.Extensions;

namespace Network.LiteNetLibBridge
{
    public class ClientBridge : IBridge
    {
        private readonly NetManager m_Manager;
        private readonly EventBasedNetListener m_Listener;

        private INetworkController m_Controller;
        private DisconnectReason? m_Reason;
        private IPEndPoint m_EndPoint;
        private NetPeer m_Peer;

        public INetworkController Controller => m_Controller;

        public IPEndPoint Target => m_EndPoint;

        public bool IsRunning => m_Manager.IsRunning;
        public bool IsConnected => m_Peer != null && m_Peer.ConnectionState == ConnectionState.Connected;

        public bool RequiresTick => true;
        public bool CanTick => m_Manager.IsRunning;

        public event Action OnStarted;
        public event Action OnStopped;

        public event Action<INetworkPeer> OnConnected;
        public event Action<INetworkPeer, object[]> OnData;
        public event Action<INetworkPeer, DisconnectReason> OnDisconnected;

        public ClientBridge(INetworkController controller)
        {
            m_Listener = new EventBasedNetListener();

            m_Listener.NetworkReceiveEvent += OnReceive;
            m_Listener.PeerConnectedEvent += OnConnection;
            m_Listener.PeerDisconnectedEvent += OnDisconnection;

            m_Manager = new NetManager(m_Listener);
            m_Controller = controller;
        }

        public void Connect(IPEndPoint endPoint)
        {
            if (IsConnected)
                Disconnect(default, new DisconnectReason("Connecting to a new server.", false));

            if (!IsRunning)
                Start();

            m_Manager.Connect(endPoint, string.Empty);
        }

        public void Disconnect(Guid guid, DisconnectReason disconnectReason)
        {
            if (IsConnected)
            {
                m_Reason = disconnectReason;
                m_Peer.Disconnect();
            }
        }

        public void Send(Guid guid, IEnumerable<object> data)
        {
            var writer = LiteTransport.Write(data);

            m_Peer.Send(writer, DeliveryMethod.ReliableOrdered);

            try
            {
                writer.Reset();
                writer = null;
            }
            catch { }
        }

        public void Start()
        {
            if (!IsRunning)
                m_Manager.Start();
        }

        public void Stop()
        {
            if (IsRunning)
                m_Manager.Stop();
        }

        public void Tick()
            => m_Manager.PollEvents();

        private void OnConnection(NetPeer peer)
        {
            m_Peer = peer;
            OnConnected.Call(null);
        }

        private void OnDisconnection(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            m_Peer = null;
            OnDisconnected.Call(null, new DisconnectReason($"Disconnected: {disconnectInfo.Reason} ({disconnectInfo.SocketErrorCode})", disconnectInfo.Reason != LiteNetLib.DisconnectReason.ConnectionRejected));
        }

        private void OnReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            if (m_Peer is null || m_Peer.Id != peer.Id)
                return;

            if (deliveryMethod != DeliveryMethod.ReliableOrdered)
                return;

            var dataObjects = LiteTransport.Read(reader);

            OnData.Call(null, dataObjects);
        }
    }
}