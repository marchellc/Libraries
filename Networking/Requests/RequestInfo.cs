using Common.Extensions;
using Common.IO.Data;

using Networking.Data;
using Networking.Enums;

using System;

namespace Networking.Requests
{
    public class RequestInfo : IData
    {
        internal Action<RequestInfo, object> handler;

        public ulong Id { get; internal set; }

        public object Value { get; internal set; }
        public object Response { get; internal set; }

        public bool IsSuccess { get; internal set; }
        public bool HasResponse { get; internal set; }

        public RequestManager Manager { get; internal set; }

        public RequestInfo() { }
        public RequestInfo(ulong id, object value)
        {
            Id = id;
            Value = value;
        }

        internal void OnResponded(object value)
        {
            if (HasResponse)
                return;

            HasResponse = true;
            Response = value;

            handler.Call(this, value);
        }

        public void Respond(bool status, object value)
        {
            if (HasResponse)
                return;

            HasResponse = true;
            IsSuccess = status;
            Response = value;

            Manager.Send(this);
        }

        public void Ok(object value)
            => Respond(true, value);

        public void Fail(object value)
            => Respond(true, value);

        public void Deserialize(DataReader reader)
        {
            Id = reader.ReadCompressedULong();
            Value = reader.ReadObject();
            HasResponse = reader.ReadBool();
            IsSuccess = reader.ReadBool();

            if (HasResponse)
                Response = reader.ReadObject();
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteCompressedULong(Id);
            writer.WriteObject(Value);
            writer.WriteBool(HasResponse);
            writer.WriteBool(IsSuccess);

            if (HasResponse)
                writer.WriteObject(Response);
        }

        public class RequestListener : BasicListener<RequestInfo>
        {
            public RequestManager Manager;

            public override void OnRegistered()
            {
                base.OnRegistered();
                Manager = Client.Get<RequestManager>();
            }

            public override void OnUnregistered()
            {
                base.OnUnregistered();
                Manager = null;
            }

            public override ListenerResult Process(RequestInfo message)
            {
                message.Manager = Manager;

                if (message.HasResponse)
                {
                    Manager.OnResponse(message);
                    return ListenerResult.Success;
                }

                Manager.OnRequest(message);
                return ListenerResult.Success;
            }
        }
    }
}