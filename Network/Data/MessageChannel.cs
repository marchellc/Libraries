using Common.Reflection;

using System;

namespace Network.Data
{
    public class MessageChannel
    {
        public const byte INTERNAL_REQUESTS = 0;
        public const byte INTERNAL_RESPONSES = 1;

        internal NetworkPeer peer;

        public MessageChannel(NetworkPeer peer, byte id)
        {
            this.peer = peer;

            Id = id;
        }

        public readonly byte Id;

        public event Action<NetworkPeer, Message> OnMessage;

        public void Send(object message, byte header = 0)
        {
            var msgType = message.GetType();
            var msgTypeId = peer.GetSyncTypes().IndexOf(msgType);

            if (msgTypeId < 0 || msgTypeId > short.MaxValue)
                throw new InvalidOperationException($"Unknown message type ID");

            peer.Send(new Message
            {
                Channel = this,

                Id = new MessageId
                {
                    Channel = Id,
                    Header = header,
                    Id = (short)msgTypeId
                },

                Type = msgType,
                Value = message
            });
        }

        public void Send(Message message)
        {
            message.Channel = this;
            message.Id.Channel = Id;

            peer.Send(message);
        }

        internal void Receive(Message message)
            => OnMessage.Call(peer, message);
    }
}