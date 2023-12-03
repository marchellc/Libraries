using Network.Interfaces.Requests;
using Network.Interfaces.Transporting;

using System;

namespace Network.Requests
{
    public struct Request : IRequest
    {
        private IResponse response;

        public byte Id { get; }

        public DateTime Sent { get; }
        public DateTime Received { get; }

        public object Object { get; }

        public IRequestManager Manager { get; }
        public IResponse Response => response;

        public bool HasResponded => response != null;

        public Request(IRequestManager manager, byte id, DateTime sent, DateTime received, object request)
        {
            Manager = manager;
            Id = id;
            Sent = sent;
            Received = received;
            Object = request;
        }

        public void Respond<T>(T response, ResponseStatus status) where T : IMessage
        {
            if (HasResponded)
                throw new InvalidOperationException($"Cannot respond to the same request twice.");

            Manager.Respond(this, response, status);
        }

        public void Success<T>(T response) where T : IMessage
            => Respond(response, ResponseStatus.Ok);

        public void Fail<T>(T response = default) where T : IMessage
            => Respond(response, ResponseStatus.Failed);

        public void OnResponded(IResponse response)
        {
            if (HasResponded)
                throw new InvalidOperationException($"Cannot respond to the same request twice.");

            this.response = response;
        }
    }
}
