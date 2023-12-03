using Network.Interfaces.Transporting;
using System;

namespace Network.Interfaces.Requests
{
    public interface IRequest
    {
        byte Id { get; }

        DateTime Sent { get; }
        DateTime Received { get; }

        object Object { get; }

        IRequestManager Manager { get; }
        IResponse Response { get; }

        bool HasResponded { get; }

        void Respond<T>(T response, ResponseStatus status) where T : IMessage;
        void Success<T>(T response) where T : IMessage;
        void Fail<T>(T response = default) where T : IMessage;

        void OnResponded(IResponse response);
    }
}