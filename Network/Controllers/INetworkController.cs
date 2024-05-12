using Network.Authentification;
using Network.Bridges;
using Network.Events;
using Network.Features;
using Network.Latency;
using Network.Targets;

using System;

namespace Network.Controllers
{
    public interface INetworkController : INetworkObject
    {
        IBridge Bridge { get; }
        INetworkEvents Events { get; }
        ILatencyMeter Latency { get; }
        INetworkTarget Target { get; set; }
        INetworkFeatureManager Features { get; }
        IAuthentification Authentification { get; }

        ControllerType Type { get; }

        TimeSpan? UpTime { get; }
        TimeSpan? ConnectedTime { get; }

        bool IsRunning { get; }
        bool IsAuthentificated { get; }
        bool IsConnected { get; }

        bool RequiresAuthentification { get; }

        string Key { get; }

        void Start();
        void Stop();

        void Connect();
        void Disconnect(DisconnectReason disconnectReason);

        void Send(params object[] args);
    }
}