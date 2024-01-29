using Common.Extensions;
using Common.IO.Data;
using Common.Pooling.Pools;

using System;

namespace Networking.Features
{
    public class NetworkFunctions
    {
        private Action<DataWriter> send;
        private Action disconnect;

        public bool IsClient { get; }
        public bool IsServer { get; }

        public NetworkFunctions(
            Action<DataWriter> send,
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

        public void Send(DataWriter writer)
            => send(writer);

        public void Send(Action<DataWriter> writer)
        {
            var net = PoolablePool<DataWriter>.Shared.Rent();

            if (net is null)
                return;

            writer.Call(net);

            Send(net);
        }

        public void Send<T>(T message)
            => Send(writer => writer.WriteObject(message));
    }
}