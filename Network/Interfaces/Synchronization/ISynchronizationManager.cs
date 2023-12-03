using System;
using System.Collections.Generic;

namespace Network.Interfaces.Synchronization
{
    public interface ISynchronizationManager
    {
        public event Action OnReady;

        IEnumerable<ISynchronizedRoot> Roots { get; }

        void Update(ISynchronizedValue value);
        void Destroy(ISynchronizedRoot root);

        TRoot Create<TRoot>() where TRoot : ISynchronizedRoot, new();

        void CreateHandler<TRoot>(Action<TRoot> handler) where TRoot : ISynchronizedRoot;
        void RemoveHandler<TRoot>(Action<TRoot> handler) where TRoot : ISynchronizedRoot;
    }
}