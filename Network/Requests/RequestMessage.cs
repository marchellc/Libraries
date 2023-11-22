using Common.Extensions;

using Network.Data;

using System;
using System.IO;

namespace Network.Requests
{
    public struct RequestMessage : IWritable, IReadable
    {
        public byte Id; 
        public object Object;

        public DateTime Sent;
        public DateTime Received;

        public RequestManager Manager;

        public RequestMessage(byte id, DateTime sent, object obj)
        {
            Id = id;
            Sent = sent;
            Object = obj;
        }

        public void Read(BinaryReader reader, NetworkPeer peer)
        {
            Id = reader.ReadByte();
            Sent = reader.ReadDate();
            Object = peer.ReadObject(reader);
            Received = DateTime.Now;
        }

        public void Write(BinaryWriter writer, NetworkPeer peer)
        {
            writer.Write(Id);
            writer.WriteDate(Sent);
            peer.WriteObject(writer, Object);
        }
    }
}