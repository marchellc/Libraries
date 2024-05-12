using Network.Authentification;
using Network.Bridges;
using Network.Callbacks;
using Network.Events;
using Network.Features;
using Network.Latency;
using Network.Peers;
using Network.Requests;
using Network.Targets;

using System;

namespace Network.Controllers.Server
{
    public class ServerPeer : INetworkPeer
    {
        public IBridge Bridge { get; }
        public ILatencyMeter Latency { get; }
        public INetworkTarget Target { get; }
        public INetworkFeatureManager Features { get; }
        public IAuthentification Authentification { get; }

        public INetworkController Controller => Bridge.Controller;

        public Guid Id { get; }

        public bool IsAuthentificated => Authentification is null || Authentification.Status == AuthentificationStatus.Authentificated;

        public ServerPeer(Guid id, IBridge bridge, INetworkTarget target, INetworkEvents networkEvents)
        {
            Id = id;
            Bridge = bridge;
            Target = target;

            Features = new NetworkFeatureManager(this, networkEvents);

            Features.AddFeature<NetworkCallbackManager>();
            Features.AddFeature<RequestManager>();

            Latency = Features.AddFeature<LatencyMeter>();

            if (bridge.Controller.RequiresAuthentification)
            {
                Authentification = Features.AddFeature<KeyAuthentification>();
                Authentification.Start(this);
            }
        }

        public void Disconnect(DisconnectReason disconnectReason)
            => Bridge.Disconnect(Id, disconnectReason);

        public void Send(params object[] args)
            => Bridge.Send(Id, args);

        public void ProcessDisconnect()
        {
            Features.RemoveFeature<ILatencyMeter>();

            if (Features.GetFeature<IAuthentification>() != null)
                Features.RemoveFeature<IAuthentification>();

            Features.RemoveFeature<INetworkCallbackManager>();
            Features.RemoveFeature<IRequestManager>();
        }

        public void Receive(object[] data)
        {
            var targets = Features.GetDataTargets();

            foreach (var msg in data)
            {
                foreach (var target in targets)
                {
                    if (target.Accepts(msg) && target.Process(msg))
                        break;
                }
            }
        }
    }
}
