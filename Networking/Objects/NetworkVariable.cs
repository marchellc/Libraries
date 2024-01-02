using Networking.Data;

using System.Collections.Concurrent;

namespace Networking.Objects
{
    public class NetworkVariable
    {
        public ConcurrentQueue<IMessage> pending = new ConcurrentQueue<IMessage>();
        public NetworkObject parent;

        public virtual void Write(Writer writer) { }
        public virtual void Read(Reader reader) { }
        public virtual void Process(IMessage msg) { }
    }
}