using Network.Controllers;
using Network.Features;
using Network.Latency;
using Network.Targets;

using System;

namespace Network.Peers
{
    public interface INetworkPeer : INetworkObject
    {
        INetworkFeatureManager Features { get; }
        INetworkController Controller { get; }
        ILatencyMeter Latency { get; }
        INetworkTarget Target { get; }

        Guid Id { get; }

        bool IsAuthentificated { get; }

        void Disconnect(DisconnectReason disconnectReason);
        void Send(params object[] args);
    }
}