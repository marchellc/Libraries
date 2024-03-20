using Common.IO.Data;

using Networking.Interfaces;

using System.Collections.Generic;

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
            => Send((IEnumerable<IData>)data);

        public void Send(IEnumerable<IData> data)
        {
            if (!CanSend)
                return;

            foreach (var obj in data)
                SendSingular(obj);
        }

        public void SendSingular(IData data)
        {
            if (data is null)
                return;

            if (!CanSend)
                return;

            var bytes = DataWriter.Write(writer =>
            {
                writer.WriteObject(data);
            });

            client.Send(bytes);
            sentBytesValue += (ulong)bytes.Length;
        }
    }
}