using Networking.Data;

using System;

namespace Networking
{
    public struct NetworkPingMessage : IMessage
    {
        public bool isServer;

        public DateTime sent;
        public DateTime recv;

        public NetworkPingMessage(bool isServer, DateTime sent, DateTime recv)
        {
            this.isServer = isServer;
            this.sent = sent;
            this.recv = recv;
        }

        public void Deserialize(Reader reader)
        {
            isServer = reader.ReadBool();

            sent = reader.ReadDate();

            if (isServer)
                recv = reader.ReadDate();
            else
                recv = DateTime.Now;
        }

        public void Serialize(Writer writer)
        {
            writer.WriteBool(isServer);
            writer.WriteDate(sent);

            if (!isServer)
            {
                writer.WriteDate(sent);
                writer.WriteDate(recv);
            }
            else
            {
                writer.WriteDate(sent);
            }
        }
    }
}