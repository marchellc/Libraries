using Common.IO.Data;

using System;

namespace Networking.Messages
{
    public struct NetworkPingMessage : IData
    {
        public bool IsFromServer;

        public DateTime Sent;
        public DateTime Received;

        public NetworkPingMessage(bool isFromServer, DateTime sent, DateTime received)
        {
            IsFromServer = isFromServer;

            Sent = sent;
            Received = received;
        }

        public void Deserialize(DataReader reader)
        {
            IsFromServer = reader.ReadBool();
            Sent = reader.ReadDate();
            Received = IsFromServer ? reader.ReadDate() : DateTime.Now;
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteBool(IsFromServer);
            writer.WriteDate(Sent);

            if (IsFromServer)
                writer.WriteDate(Sent);
            else
            {
                writer.WriteDate(Sent);
                writer.WriteDate(Received);
            }
        }
    }
}