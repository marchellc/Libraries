using Common.Logging;
using Common.Utilities;

using Network.Authentification;
using Network.Bridges;
using Network.Callbacks;
using Network.Data;
using Network.Events;
using Network.Features;
using Network.Latency;
using Network.Peers;
using Network.Reconnection;
using Network.Requests;
using Network.Targets;
using Network.Targets.Ip;

using System;

namespace Network.Controllers.Client
{
    public class ClientController : INetworkController
    {
        private readonly BasicEvents m_PrivateEvents;
        private readonly BasicEvents m_PublicEvents;
        private readonly IBridge m_Bridge;
        private readonly LogOutput m_Log;

        private IAuthentification m_Authentification;
        private INetworkFeatureManager m_Features;
        private IDataManager m_DataManager;
        private INetworkTarget m_Target;
        private ILatencyMeter m_Latency;

        private DateTime m_Started;
        private DateTime m_Connected;

        public INetworkEvents Events => m_PublicEvents;
        public ILatencyMeter Latency => m_Latency;
        public IBridge Bridge => m_Bridge;

        public INetworkTarget Target { get => m_Target; set => m_Target = value; }
        public INetworkFeatureManager Features => m_Features;

        public IAuthentification Authentification => m_Authentification;

        public ControllerType Type { get; } = ControllerType.Client;

        public TimeSpan? UpTime => IsRunning ? (DateTime.Now - m_Started) : null;
        public TimeSpan? ConnectedTime => IsConnected ? (DateTime.Now - m_Connected) : null;

        public bool IsRunning => m_Bridge.IsRunning;
        public bool IsConnected => m_Bridge.IsConnected;
        public bool IsAuthentificated => m_Authentification.Status == AuthentificationStatus.Authentificated;

        public bool RequiresAuthentification { get; }

        public string Key { get; }

        public ClientController(IBridge bridge, string clientKey = null, bool useReconnector = false)
        {
            if (bridge is null)
                throw new ArgumentNullException(nameof(bridge));

            m_Log = new LogOutput("Network Client");
            m_Log.Setup();

            Key = clientKey;
            RequiresAuthentification = !string.IsNullOrWhiteSpace(clientKey);

            m_PublicEvents = new BasicEvents();
            m_PrivateEvents = new BasicEvents();
            m_Features = new NetworkFeatureManager(this, m_PrivateEvents);
            m_Target = IPTarget.GetLocalLoopback();

            m_DataManager = m_Features.AddFeature<DataManager>();
            m_Authentification = RequiresAuthentification ? m_Features.AddFeature<KeyAuthentification>() : null;

            m_Features.AddFeature<NetworkCallbackManager>();
            m_Features.AddFeature<RequestManager>();

            m_Latency = m_Features.AddFeature<LatencyMeter>();

            if (useReconnector)
                m_Features.AddFeature<Reconnector>();

            if (bridge.RequiresTick)
            {
                CodeUtils.OnThread(() =>
                {
                    while (true)
                    {
                        if (m_Bridge != null && m_Bridge.CanTick)
                            m_Bridge.Tick();
                    }
                });
            }

            bridge.OnStarted += OnBridgeStarted;
            bridge.OnStopped += OnBridgeStopped;
            bridge.OnConnected += OnBridgeConnected;
            bridge.OnDisconnected += OnBridgeDisconnected;
            bridge.OnData += Receive;
        }

        public void Connect()
        {
            if (m_Target != null)
            {
                if (IsRunning)
                    Stop();

                Start();

                m_PrivateEvents.Connecting(m_Target);
                m_PublicEvents.Connecting(m_Target);

                m_Bridge.Connect(m_Target.IPEndPoint);
            }
        }

        public void Disconnect(DisconnectReason reason)
        {
            if (IsRunning)
            {
                m_PrivateEvents.Disconnecting(null, reason);
                m_PublicEvents.Disconnecting(null, reason);
                m_Bridge.Disconnect(default, reason);
            }
        }

        public void Start()
        {
            if (!IsRunning)
            {
                m_PrivateEvents.Starting();
                m_PublicEvents.Starting();
                m_Bridge.Start();
            }
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
            => m_Bridge.Send(default, args);

        public void Send(Guid guid, params object[] args)
            => throw new InvalidOperationException($"Cannot send to a specific peer from the client.");

        public bool TryGetPeer(Guid guid, out INetworkPeer peer)
            => throw new InvalidOperationException($"Cannot retreive peers from the client.");

        private void Receive(INetworkPeer peer, object controllerData)
        {
            try
            {
                if (controllerData is null)
                {
                    m_Log.Warn($"Received null data!");
                    return;
                }

                if (RequiresAuthentification && !IsAuthentificated && !m_Authentification.Receive(controllerData))
                {
                    m_Log.Warn($"Attempted to process data on an unauthentificated client!");
                    return;
                }

                if (InternalProcess(controllerData))
                    return;

                var dataTargets = m_Features.GetDataTargets();

                if (dataTargets.Count < 1)
                    return;

                foreach (var target in dataTargets)
                {
                    if (target.Accepts(controllerData) && target.Process(controllerData))
                        return;
                }

                m_Log.Warn($"No handlers were available for data '{controllerData.GetType().FullName}'");
            }
            catch (Exception ex)
            {
                m_Log.Error($"Caught an error while receiving data: {ex}");
            }
        }

        private bool InternalProcess(object data)
            => false;

        private void OnBridgeDisconnected(INetworkPeer obj, DisconnectReason reason)
        {
            m_Log.Warn($"The network bridge has disconnected from {m_Bridge.Target}: {reason}");
            m_PrivateEvents.Disconnected(obj, reason);
            m_PublicEvents.Disconnected(obj, reason);
            m_Connected = DateTime.MinValue;
        }

        private void OnBridgeConnected(INetworkPeer obj)
        {
            m_Log.Info($"The network bridge has connected to {m_Bridge.Target}.");
            m_PrivateEvents.Connected(obj);
            m_PublicEvents.Connected(obj);
            m_Connected = DateTime.Now;
        }

        private void OnBridgeStopped()
        {
            m_Log.Info("The network bridge has stoppped.");
            m_PrivateEvents.Stopped();
            m_PublicEvents.Stopped();
            m_Started = DateTime.MinValue;
        }

        private void OnBridgeStarted()
        {
            m_Log.Info("The network bridge has started.");
            m_PrivateEvents.Started();
            m_PublicEvents.Started();
            m_Started = DateTime.Now;
        }
    }
}
