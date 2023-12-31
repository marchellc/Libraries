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

        public bool isClient;
        public bool isServer;

        public NetworkFunctions(
            Func<Writer> getWriter, 
            Func<byte[], Reader> getReader, 
            
            Action<Writer> sendWriter,
            
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
            this.isClient = isClient;
            this.isServer = !isClient;
        }

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
            => Send(writer => 
            {
                var msgs = messages.Where(msg => msg != null && msg is ISerialize);
                var size = msgs.Count();

                if (size <= 0)
                    return;

                writer.WriteInt(size);

                foreach (var msg in msgs)
                    (msg as ISerialize).Serialize(writer);
            });
    }
}