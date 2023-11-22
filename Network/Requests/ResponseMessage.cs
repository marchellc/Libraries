using Common.Extensions;

using Network.Data;

using System;
using System.IO;

namespace Network.Requests
{
    public struct ResponseMessage : IReadable, IWritable
    {
        public byte Id;
        public object Response;

        public DateTime RequestSent;
        public DateTime RequestReceived;

        public DateTime ResponseSent;
        public DateTime ResponseReceived;

        public ResponseStatus Status;

        public ResponseMessage(byte id, object response, RequestMessage request)
        {
            Id = id;
            Response = response;
            RequestReceived = request.Sent;
            RequestReceived = request.Received;
        }

        public void Read(BinaryReader reader, NetworkPeer peer)
        {
            Id = reader.ReadByte();
            Response = peer.ReadObject(reader);

            RequestSent = reader.ReadDate();
            RequestReceived = reader.ReadDate();

            ResponseSent = reader.ReadDate();
            ResponseReceived = DateTime.Now;

            Status = ResponseStatus.Received;
        }

        public void Write(BinaryWriter writer, NetworkPeer peer)
        {
            writer.Write(Id);

            peer.WriteObject(writer, Response);

            writer.WriteDate(RequestSent);
            writer.WriteDate(RequestReceived);

            writer.WriteDate(ResponseSent);
        }
    }
}