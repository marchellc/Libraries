using Common.IO.Data;

using System;

namespace Networking.Requests
{
    public struct ResponseInfo : IData
    {
        public string Id { get; private set; }

        public object Response { get; private set; }

        public bool IsSuccess { get; private set; }

        public DateTime SentAt { get; private set; }
        public DateTime ReceivedAt { get; private set; }

        public RequestManager Manager { get; internal set; }

        public ResponseInfo(string id, object response, bool isSuccess)
        {
            Id = id;
            Response = response;
            IsSuccess = isSuccess;
            SentAt = DateTime.Now;
        }

        public void Deserialize(DataReader reader)
        {
            Id = reader.ReadString();
            Response = reader.ReadObject();
            SentAt = reader.ReadDate();
            IsSuccess = reader.ReadBool();
            ReceivedAt = DateTime.Now;
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteString(Id);
            writer.WriteObject(Response);
            writer.WriteDate(SentAt);
            writer.WriteBool(IsSuccess);
        }
    }
}