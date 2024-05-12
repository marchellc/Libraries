using Common.Extensions;

using Network.Authentification;
using Network.Bridges;
using Network.Data;
using Network.Events;
using Network.Features;
using Network.Latency;
using Network.Peers;
using Network.Targets;

using System;

namespace Network.Controllers.Server
{
    public class ServerController : INetworkController
    {
        private readonly BasicEvents m_PrivateEvents;
        private readonly BasicEvents m_PublicEvents;
        private readonly IBridge m_Bridge;

        private IDataManager m_DataManager;
        private INetworkFeatureManager m_Features;
        private IAuthentification m_Authentification;

        private DateTime m_Started;
        private DateTime m_Connected;

        public ILatencyMeter Latency => null;

        public IBridge Bridge => m_Bridge;
        public INetworkEvents Events => m_PublicEvents;
        public INetworkFeatureManager Features => m_Features;
        public IAuthentification Authentification => m_Authentification;

        public INetworkTarget Target { get; set; }

        public ControllerType Type { get; } = ControllerType.Server;

        public TimeSpan? UpTime => IsRunning ? (DateTime.Now - m_Started) : null;
        public TimeSpan? ConnectedTime => IsConnected ? (DateTime.Now - m_Connected) : null;

        public bool IsRunning => m_Bridge.IsRunning;
        public bool IsConnected => m_Bridge.IsConnected;
        public bool IsAuthentificated => true;

        public bool RequiresAuthentification => m_Authentification != null;

        public string Key => null;

        public ServerController(IBridge bridge, bool useAuth = false)
        {
            m_Bridge = bridge;

            m_PrivateEvents = new BasicEvents();
            m_PublicEvents = new BasicEvents();

            m_Features = new NetworkFeatureManager(this, m_PrivateEvents);
            m_DataManager = m_Features.AddFeature<DataManager>();

            m_Bridge.OnStarted += OnBridgeStarted;
            m_Bridge.OnStopped += OnBridgeStopped;
            m_Bridge.OnConnected += OnBridgeConnected;
        }

        private void OnBridgeConnected(INetworkPeer obj)
        {
            m_PrivateEvents.Connected(obj);
            m_PublicEvents.Connected(obj);

            if (m_Connected == DateTime.MinValue)
                m_Connected = DateTime.Now;
        }

        private void OnBridgeStopped()
        {
            m_PrivateEvents.Stopped();
            m_PublicEvents.Stopped();
            m_Started = DateTime.MinValue;
            m_Connected = DateTime.MinValue;
        }

        private void OnBridgeStarted()
        {
            m_PrivateEvents.Started();
            m_PublicEvents.Started();
            m_Started = DateTime.Now;
            m_Connected = DateTime.MinValue;
        }

        public void Connect()
            => throw new InvalidOperationException("The server cannot connect.");

        public void Disconnect(DisconnectReason disconnectReason)
            => throw new InvalidOperationException($"The server cannot disconnect.");

        public void Start()
        {
            if (IsRunning)
                Stop();

            m_PrivateEvents.Starting();
            m_PublicEvents.Starting();
            m_Bridge.Start();
        }

        public void Stop()
        {
            if (IsRunning)
            {
                m_PrivateEvents.Stopping();
                m_PublicEvents.Stopping();
                m_Bridge.Stop();
            }
        }

        public void Send(params object[] args)
            => throw new InvalidOperationException("The server cannot send data");

        public void Receive(object controllerData)
            => throw new InvalidOperationException($"The server cannot process data");
    }
}
