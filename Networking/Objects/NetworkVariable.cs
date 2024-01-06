using Networking.Data;

using System.Collections.Concurrent;

namespace Networking.Objects
{
    public class NetworkVariable
    {
        public NetworkObject parent;
        public ConcurrentQueue<IMessage> pending = new ConcurrentQueue<IMessage>();

        public virtual void Process(IMessage msg) { }
    }
}