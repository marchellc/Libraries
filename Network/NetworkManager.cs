using Network.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Network
{
    public class NetworkManager
    {
        private List<Type> features = new List<Type>();
        private List<MessageChannel> channels = new List<MessageChannel>();

        public virtual bool IsInitialized { get; }
        public virtual bool IsRunning { get; }
        public virtual bool IsConnected { get; }

        public virtual NetworkPeer Peer { get; }

        public virtual IPEndPoint EndPoint { get; }

        public virtual void Start() { }
        public virtual void Stop() { }

        public virtual void Connect() { }
        public virtual void Disconnect() { }

        public virtual void Send(int id, ArraySegment<byte> data) { }

        public void AddChannel(byte channelId)
        {
            if (channels.Any(c => c.Id == channelId))
                return;

            var channel = new MessageChannel(null, channelId);

            channels.Add(channel);
        }

        public void AddFeature<TFeature>() where TFeature : NetworkFeature, new()
        {
            if (features.Any(f => f == typeof(TFeature)))
                return;

            features.Add(typeof(TFeature));
        }

        internal Type[] GetFeatures()
            => features.ToArray();

        internal MessageChannel[] GetChannels()
            => channels.ToArray();
    }
}