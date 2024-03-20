using Common.IO.Data;

using Networking.Interfaces;

using System.Collections.Generic;
using System.Linq;

namespace Networking.Data
{
    public class NetSender : ISender
    {
        private ulong sentBytesValue;
        private IClient client;

        public bool CanSend => client != null && client.IsConnected;

        public ulong SentBytes => sentBytesValue;

        public NetSender(IClient client)
        {
            this.client = client;
        }

        public void Send(params IData[] data)
        {
            if (!CanSend)
                return;

            var bytes = DataWriter.Write(writer =>
            {
                var pack = new NetPack(data);
                writer.Write(pack);
            });

            client.Send(bytes);
            sentBytesValue += (ulong)bytes.Length;
        }

        public void Send(IEnumerable<IData> data)
        {
            if (!CanSend)
                return;

            var bytes = DataWriter.Write(writer =>
            {
                var batch = data.ToArray();
                var pack = new NetPack(batch);

                writer.Write(pack);
            });

            client.Send(bytes);
            sentBytesValue += (ulong)bytes.Length;
        }

        public void SendSingular(IData data)
        {
            if (data is null)
                return;

            if (!CanSend)
                return;

            InternalSend(data);
        }

        private void InternalSend(IData data)
        {
            if (!CanSend)
                return;

            var bytes = DataWriter.Write(writer =>
            {
                var batch = new IData[] { data };
                var pack = new NetPack(batch);

                writer.Write(pack);
            });

            client.Send(bytes);
            sentBytesValue += (ulong)bytes.Length;
        }
    }
}