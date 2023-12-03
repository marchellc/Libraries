using System;

namespace Network.Interfaces.Requests
{
    public interface IResponse
    {
        IRequest Request { get; }

        DateTime Sent { get; }
        DateTime Received { get; }

        ResponseStatus Status { get; }

        object Object { get; }

        bool IsHandled { get; }

        IRequestManager Manager { get; }

        void OnHandled();
    }
}