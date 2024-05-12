using Network.Data;
using Network.Features;
using Network.Peers;

using System;

namespace Network.Authentification
{
    public interface IAuthentification : INetworkFeature, IDataTarget
    {
        IAuthentificationStorage Storage { get; }
        IAuthentificationData Data { get; }

        AuthentificationStatus Status { get; }
        AuthentificationFailureReason FailureReason { get; }

        DateTime SentAt { get; }
        DateTime ReceivedAt { get; }

        double TimeRequired { get; }

        void Start(INetworkPeer peer);
    }
}