using Common.Extensions;
using Common.IO.Data;
using Common.Logging;

using Networking.Data;
using Networking.Enums;
using Networking.Interfaces;

using System;

namespace Networking
{
    public class NetComponent : IComponent
    {
        private LogOutput logValue;

        public ISender Sender { get; set; }
        public IClient Client { get; set; }

        public bool IsRunning { get; set; }

        public bool IsServer => Client.Type is ClientType.Peer;
        public bool IsClient => Client.Type is ClientType.Client;

        public NetListener Listener { get; set; }

        public LogOutput Log
        {
            get
            {
                if (logValue is null)
                {
                    logValue = new LogOutput($"{GetType().Name} ({Client.RemoteAddress})").Setup();
                    logValue.Info($"Initialized logging.");
                }

                return logValue;
            }
        }

        public virtual void Start() { }
        public virtual void Stop() { }

        public virtual void OnMessage(string messageId, object value) { }

        public void Send<T>(T data) where T : IData
            => Client.Sender.SendSingular(data);

        public void SendMessage(string messageId, object messageValue)
            => Client.Sender.SendSingular(new WrappedMessage(GetType(), messageId, messageValue));

        public struct WrappedMessage : IData
        {
            public Type ComponentType;
            public string MessageId;
            public object MessageObject;

            public WrappedMessage(Type componentType, string messageId, object messageObject)
            {
                ComponentType = componentType;
                MessageId = messageId;
                MessageObject = messageObject;
            }

            public void Deserialize(DataReader reader)
            {
                ComponentType = reader.ReadType();
                MessageId = reader.ReadString();
                MessageObject = reader.ReadObject();
            }

            public void Serialize(DataWriter writer)
            {
                writer.WriteType(ComponentType);
                writer.WriteString(MessageId);
                writer.WriteObject(MessageObject);
            }

            public class WrappedMessageListener : BasicListener<WrappedMessage>
            {
                public override ListenerResult Process(WrappedMessage message)
                {
                    if (!Client.Components.TryGetFirst(comp => comp.GetType() == message.ComponentType, out var component) || component is not NetComponent netComponent)
                        return ListenerResult.Success;

                    netComponent.OnMessage(message.MessageId, message.MessageObject);
                    return ListenerResult.Success;
                }
            }
        }
    }
}