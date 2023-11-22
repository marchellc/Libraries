using Network.Data;

namespace Network
{
    public class NetworkFeature
    {
        public NetworkPeer Peer { get; private set; }

        public virtual byte Priority { get; } = 100;

        public virtual void Start() { }
        public virtual void Stop() { }

        public virtual void Receive(Message message) { }

        internal void StartInternal(NetworkPeer peer)
        {
            Peer = peer;
            Start();
        }

        internal void StopInternal()
        {
            Stop();
            Peer = null;
        }
    }
}