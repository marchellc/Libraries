using Network.Interfaces.Requests;

using System;

namespace Network.Requests
{
    public struct Response : IResponse
    {
        private bool isHandled;

        public IRequest Request { get; }

        public DateTime Sent { get; }
        public DateTime Received { get; }

        public ResponseStatus Status { get; }

        public object Object { get; }

        public bool IsHandled => isHandled;

        public IRequestManager Manager { get; }

        public Response(IRequestManager manager, IRequest request, DateTime sent, DateTime received, ResponseStatus status, object response)
        {
            Manager = manager;
            Request = request;
            Sent = sent;
            Received = received;
            Status = status;
            Object = response;
        }

        public void OnHandled()
            => isHandled = true;
    }
}