using Networking.Data;

using System;

namespace Networking.Requests
{
    public class RequestInfo : IMessage
    {
        public RequestInfo() { }

        public string id;

        public DateTime sentAt;
        public DateTime receivedAt;

        public object value;

        public ResponseInfo response;
        public RequestManager manager;

        public bool isResponded;
        public bool isTimedOut;

        public void Deserialize(Reader reader)
        {
            id = reader.ReadCleanString();

            sentAt = reader.ReadDate();
            receivedAt = DateTime.Now;

            value = reader.ReadAnonymous();
        }

        public void Serialize(Writer writer)
        {
            writer.WriteString(id);
            writer.WriteDate(DateTime.Now);
            writer.WriteAnonymous(value);
        }
    }
}