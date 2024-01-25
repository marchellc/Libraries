using Networking.Data;

using System;

namespace Networking.Requests
{
    public class ResponseInfo : IMessage
    {
        public ResponseInfo() { }

        public RequestInfo request;
        public RequestManager manager;

        public DateTime sentAt;
        public DateTime receivedAt;

        public object response;
        public bool isSuccess;
        public string id;

        public void Deserialize(Reader reader)
        {
            sentAt = reader.ReadDate();
            receivedAt = DateTime.Now;

            id = reader.ReadString();

            isSuccess = reader.ReadBool();

            response = reader.ReadAnonymous();
        }

        public void Serialize(Writer writer)
        {
            writer.WriteDate(DateTime.Now);
            writer.WriteString(id);
            writer.WriteBool(isSuccess);
            writer.WriteAnonymous(response);
        }
    }
}