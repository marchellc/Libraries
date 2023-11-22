using Common.Reflection;

using Network.Data;

using System;
using System.Collections.Generic;
using System.IO;

namespace Network
{
    public class NetworkPeer
    {
        public virtual int Id { get; }

        public virtual bool IsInitialized { get; }
        public virtual bool IsRunning { get; }
        public virtual bool IsConnected { get; }

        public virtual short Latency { get; }

        public virtual string Address { get; }

        public virtual NetworkManager Manager { get; }

        public virtual IReadOnlyList<NetworkFeature> Features { get; }
        public virtual IReadOnlyList<MessageChannel> Channels { get; }

        public event Action<long> OnLatencyRequested;
        public event Action<long, short> OnLatencyReceived;

        public virtual void Start() { }
        public virtual void Stop() { }

        public virtual void Send(Message message) { }
        public virtual void Send(MessageId message) { }
        public virtual void Send(MessageId message, Action<BinaryWriter> write) { }
        public virtual void Send(ArraySegment<byte> data) { }

        public virtual void Receive(ArraySegment<byte> data) { }

        public void Send<TMessage>(TMessage message, MessageChannel channel = null) where TMessage : IWritable
        {
            if (message is null)
                return;

            var msgType = message.GetType();
            var msgIndex = (short)GetSyncTypes().IndexOf(msgType);

            if (msgIndex < 0)
                return;

            Send(new Message
            {
                Channel = channel,
                Type = msgType,
                Value = message,

                Id = new MessageId
                {
                    Channel = channel?.Id ?? 0,
                    Header = 0,
                    Id = msgIndex
                }
            });
        }

        public virtual object ReadObject(BinaryReader reader) { return null; }
        public virtual void WriteObject(BinaryWriter writer, object instance) { }

        internal virtual List<Type> GetSyncTypes() { return null; }

        public TFeature GetFeature<TFeature>() where TFeature : NetworkFeature, new()
        {
            for (int i = 0; i < Features.Count; i++)
            {
                if (Features[i] is TFeature feature)
                    return feature;
            }

            return default;
        }

        public TFeature GetOrAddFeature<TFeature>() where TFeature : NetworkFeature, new()
        {
            var feature = GetFeature<TFeature>();

            if (feature != null)
                return feature;

            AddFeature(feature = new TFeature());

            return feature;
        }

        public void RemoveFeature<TFeature>() where TFeature : NetworkFeature, new()
        {
            var feature = GetFeature<TFeature>();

            if (feature is null)
                return;

            RemoveFeature(feature);
        }

        public virtual void AddFeature(NetworkFeature feature) { }
        public virtual void RemoveFeature(NetworkFeature feature) { }

        public virtual void Handle(MessageId message, Action<BinaryReader> handler) { }
        public virtual void Handle<TMessage>(Action<Message, TMessage> handler) where TMessage : IWritable { }

        public virtual void RemoveHandle(Action<BinaryReader> handler) { }
        public virtual void RemoveHandle<TMessage>(Action<Message, TMessage> handler) where TMessage : IWritable { }

        internal void InvokeLatencyReceived(long sentAt, short latency)
            => OnLatencyReceived.Call(sentAt, latency);

        internal void InvokeLatencyRequested(long requestAt)
            => OnLatencyRequested.Call(requestAt);
    }
}