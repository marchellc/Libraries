using Network.Features;

using System.Collections.Generic;

namespace Network.Blocking
{
    public interface IBlock<TTarget> : INetworkFeature
    {
        BlockMode Mode { get; }

        IEnumerable<TTarget> List { get; }

        bool IsAllowed(TTarget target);

        void Add(TTarget target);
        void Remove(TTarget target);
    }
}