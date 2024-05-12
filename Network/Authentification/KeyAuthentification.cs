using Network.Features;
using Network.Peers;

using System;

namespace Network.Authentification
{
    public class KeyAuthentification : NetworkFeatureBase, IAuthentification
    {
        public IAuthentificationStorage Storage { get; }
        public IAuthentificationData Data { get; }

        public AuthentificationStatus Status { get; private set; }
        public AuthentificationFailureReason FailureReason { get; private set; }

        public DateTime SentAt { get; private set; }
        public DateTime ReceivedAt { get; private set; }

        public double TimeRequired { get; private set; }

        public void Start(INetworkPeer peer)
        {

        }

        public bool Accepts(object data)
            => true;

        public bool Process(object data)
            => false;
    }
}