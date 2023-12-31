using Common.IO.Collections;

using Networking.Data;

namespace Networking.Objects
{
    public class NetworkVariable
    {
        public LockedList<IMessage> pending = new LockedList<IMessage>();
        public NetworkObject parent;

        public virtual void Write(Writer writer) { }
        public virtual void Read(Reader reader) { }
        public virtual void Process(IDeserialize deserialize) { }
    }
}