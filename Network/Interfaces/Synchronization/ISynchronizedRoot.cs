using System.Collections.Generic;

namespace Network.Interfaces.Synchronization
{
    public interface ISynchronizedRoot
    {
        public IEnumerable<ISynchronizedValue> Values { get; }

        public ISynchronizationManager Manager { get; }

        public short Id { get; }

        void OnCreated();
        void OnDestroyed();

        ISynchronizedValue ValueOfId(short id);
    }
}