using Common.IO.Data;

using System;

namespace Networking.Requests
{
    public struct RequestInfo : IData
    {
        public string Id { get; private set; }

        public object Request { get; private set; }

        public DateTime SentAt { get; private set; }
        public DateTime ReceivedAt { get; private set; }

        public RequestManager Manager { get; internal set; }
        public ResponseInfo Response { get; internal set; }

        public bool IsResponded { get; internal set; }

        public RequestInfo(string id, object request)
        {
            Id = id;
            Request = request;
            SentAt = DateTime.Now;
        }

        public void Deserialize(DataReader reader)
        {
            Id = reader.ReadString();
            Request = reader.ReadObject();
            SentAt = reader.ReadDate();
            ReceivedAt = DateTime.Now;
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteString(Id);
            writer.WriteObject(Request);
            writer.WriteDate(SentAt);
        }

        public void Respond(object response, bool success)
        {
            if (IsResponded)
                throw new InvalidOperationException($"This request has already been responded to!");

            Manager.Respond(this, response, success);
        }

        public void RespondOk(object response = null)
            => Respond(response, true);

        public void RespondFail(object response = null)
            => Respond(response, false);
    }
}
