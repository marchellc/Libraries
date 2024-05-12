using Common.IO.Data;

using System;

namespace Network.Requests
{
    public class ResponseBase : IResponse, IData
    {
        private bool m_Success;

        private string m_RequestId;
        private object m_Response;

        private DateTime m_SentAt;
        private DateTime m_RecvAt;

        public bool IsSuccess => m_Success;

        public string RequestId => m_RequestId;
        public object Response => m_Response;

        public DateTime ReceivedAt => m_RecvAt;
        public DateTime SentAt => m_SentAt;

        public ResponseBase() { }

        public ResponseBase(bool success, string id, object response)
        {
            m_Success = success;
            m_RequestId = id;
            m_Response = response;
        }

        public void Deserialize(DataReader reader)
        {
            m_Success = reader.ReadBool();
            m_RequestId = reader.ReadString();
            m_Response = reader.ReadObject();
            m_SentAt = reader.ReadDate();
            m_RecvAt = DateTime.Now;
        }

        public void Serialize(DataWriter writer)
        {
            m_SentAt = DateTime.Now;

            writer.WriteBool(m_Success);
            writer.WriteString(m_RequestId);
            writer.WriteObject(m_Response);
            writer.WriteDate(m_SentAt);
        }
    }
}
