using Common.Extensions;

using Networking.Data;
using Networking.Pooling;
using System;

namespace Networking.Features
{
    public class NetworkFunctions
    {
        private Action<Writer> send;
        private Action disconnect;

        public bool IsClient { get; }
        public bool IsServer { get; }

        public NetworkFunctions(
            Action<Writer> send,
            Action disconnect,
            
            bool isClient)
        {
            if (send is null)
                throw new ArgumentNullException(nameof(send));

            this.send = send;
            this.disconnect = disconnect;

            IsClient = isClient;
            IsServer = !isClient;
        }

        public void Disconnect()
            => disconnect();

        public void Send(Writer writer)
            => send(writer);

        public void Send(Action<Writer> writer)
        {
            var net = WriterPool.Shared.Rent();

            if (net is null)
                return;

            writer.Call(net);

            Send(net);
        }

        public void Send(params object[] messages)
            => Send(writer => writer.WriteAnonymousArray(messages));
    }
}