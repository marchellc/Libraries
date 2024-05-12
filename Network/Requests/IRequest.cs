using Common.IO.Data;

using System;

namespace Network.Requests
{
    public interface IRequest : IData
    {
        string Id { get; }

        object Request { get; }

        bool IsDeclined { get; }

        DateTime SentAt { get; }
        DateTime ReceivedAt { get; }

        INetworkObject AcceptedBy { get; }

        void Accept(INetworkObject networkObject);

        void RespondFail(object response);
        void RespondSuccess(object response);

        void Decline();
    }
}