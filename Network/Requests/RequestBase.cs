using Common.IO.Data;

using Network.Features;

using System;

namespace Network.Requests
{
    public class RequestBase : IRequest, IData
    {
        private string m_Id;
        private object m_Request;

        private DateTime m_SentAt;
        private DateTime m_RecvAt;

        private INetworkObject m_Accepted;

        public string Id => m_Id;
        public object Request => m_Request;

        public bool IsDeclined => m_Accepted is null;

        public DateTime SentAt => m_SentAt;
        public DateTime ReceivedAt => m_RecvAt;

        public INetworkObject AcceptedBy => m_Accepted;

        public RequestBase() { }

        public RequestBase(string id, object request)
        {
            m_Id = id;
            m_Request = request;
        }

        public void Accept(INetworkObject networkObject)
        {
            if (m_Accepted != null)
                throw new InvalidOperationException($"This request has already been accepted.");

            m_Accepted = networkObject;
        }

        public void Decline()
        {
            if (m_Accepted != null)
                throw new InvalidOperationException($"This request has already been accepted.");

            m_Accepted = null;
        }

        public void RespondFail(object response)
        {
            if (m_Accepted is null)
                throw new InvalidOperationException($"This request has not been processed.");

            m_Accepted.ExecuteFeature<IRequestManager>(req => req.Send(response, this, false));
        }

        public void RespondSuccess(object response)
        {
            if (m_Accepted is null)
                throw new InvalidOperationException($"This request has not been processed.");

            m_Accepted.ExecuteFeature<IRequestManager>(req => req.Send(response, this, true));
        }

        public void Deserialize(DataReader reader)
        {
            m_RecvAt = DateTime.Now;
            m_Id = reader.ReadString();
            m_Request = reader.ReadObject();
            m_SentAt = reader.ReadDate();
        }

        public void Serialize(DataWriter writer)
        {
            m_SentAt = DateTime.Now;

            writer.WriteString(m_Id);
            writer.WriteObject(m_Request);
            writer.WriteDate(m_SentAt);
        }
    }
}