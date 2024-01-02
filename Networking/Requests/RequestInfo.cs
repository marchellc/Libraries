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

        public void Respond(object response, bool isSuccess)
        {
            if (isResponded)
                return;

            this.manager.Respond(this, response, isSuccess);
        }

        public void Success(object response)
            => Respond(response, true);

        public void Fail(object response = null)
            => Respond(response, false);

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