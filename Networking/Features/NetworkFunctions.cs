using Common.Extensions;

using Networking.Data;

using System;
using System.Linq;

namespace Networking.Features
{
    public class NetworkFunctions
    {
        private Func<Writer> getWriter;
        private Func<byte[], Reader> getReader;

        private Action<Writer> sendWriter;
        private Action disconnect;

        public bool isClient;
        public bool isServer;

        public NetworkFunctions(
            Func<Writer> getWriter, 
            Func<byte[], Reader> getReader, 
            
            Action<Writer> sendWriter,
            Action disconnect,
            
            bool isClient)
        {
            if (getWriter is null)
                throw new ArgumentNullException(nameof(getWriter));

            if (getReader is null)
                throw new ArgumentNullException(nameof(getReader));

            if (sendWriter is null)
                throw new ArgumentNullException(nameof(sendWriter));

            this.getWriter = getWriter;
            this.getReader = getReader;
            this.sendWriter = sendWriter;
            this.disconnect = disconnect;
            this.isClient = isClient;
            this.isServer = !isClient;
        }

        public void Disconnect()
            => disconnect();

        public Writer GetWriter()
            => getWriter();

        public Reader GetReader(byte[] data)
            => getReader(data);

        public void Send(Writer writer)
            => sendWriter(writer);

        public void Send(Action<Writer> writer)
        {
            var net = GetWriter();

            if (net is null)
                return;

            writer.Call(net);

            Send(net);
        }

        public void Send(params object[] messages)
            => Send(writer => writer.WriteAnonymousArray(messages));
    }
}